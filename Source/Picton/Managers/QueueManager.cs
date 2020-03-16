using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using MessagePack;
using Picton.Interfaces;
using Picton.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	public class QueueManager : IQueueManager
	{
		#region FIELDS

		private const sbyte LZ4_MESSAGEPACK_SERIALIZATION = 99;
		private const sbyte TYPELESS_MESSAGEPACK_SERIALIZATION = 100;

		private readonly QueueClient _queue;
		private readonly BlobContainerClient _blobContainer;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Initializes a new instance of the <see cref="QueueManager"/> class.
		/// </summary>
		/// <param name="connectionString">
		/// A connection string includes the authentication information
		/// required for your application to access data in an Azure Storage
		/// account at runtime.
		///
		/// For more information, <see href="https://docs.microsoft.com/en-us/azure/storage/common/storage-configure-connection-string"/>.
		/// </param>
		/// <param name="queueName">The name of the queue in the storage account to reference.</param>
		[ExcludeFromCodeCoverage]
		public QueueManager(string connectionString, string queueName)
		{
			if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
			if (string.IsNullOrEmpty(queueName)) throw new ArgumentNullException(nameof(queueName));

			_blobContainer = new BlobContainerClient(connectionString, $"{queueName}-oversized-messages");
			_queue = new QueueClient(connectionString, queueName);

			Init();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueueManager"/> class.
		/// </summary>
		/// <param name="blobContainerClient">The blob container.</param>
		/// <param name="queueClient">The queue client.</param>
		public QueueManager(BlobContainerClient blobContainerClient, QueueClient queueClient)
		{
			_blobContainer = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
			_queue = queueClient ?? throw new ArgumentNullException(nameof(queueClient));

			Init();
		}

		#endregion

		#region PUBLIC METHODS

		public async Task AddMessageAsync<T>(T message, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = null, TimeSpan? initialVisibilityDelay = null, CancellationToken cancellationToken = default)
		{
			if (message == null) return;

			var data = Serialize(message, metadata);

			// Check if the message exceeds the size allowed by Azure Storage queues
			if (data.Length > _queue.MessageMaxBytes)
			{
				// The message is too large. Therefore we must save the content to blob storage and
				// send a smaller message indicating where the actual message was saved

				// 1) Save the large message to blob storage
				var blobName = $"{DateTime.UtcNow.ToString("yyyy-MM-dd-HH-mm-ss")}-{RandomGenerator.GenerateString(32)}";
				var blob = _blobContainer.GetBlobClient(blobName);
				await blob.UploadTextAsync(data, null, null, null, null, cancellationToken).ConfigureAwait(false);

				// 2) Send a smaller message
				var largeEnvelope = new LargeMessageEnvelope
				{
					BlobName = blobName,
					Version = typeof(QueueManager).GetTypeInfo().Assembly.GetName().Version
				};
				data = Serialize(largeEnvelope, null);

				await _queue.SendMessageAsync(data, initialVisibilityDelay, timeToLive, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				// The size of this message is within the range allowed by Azure Storage queues
				await _queue.SendMessageAsync(data, initialVisibilityDelay, timeToLive, cancellationToken).ConfigureAwait(false);
			}
		}

		public Task ClearAsync(CancellationToken cancellationToken = default)
		{
			return _queue.ClearMessagesAsync(cancellationToken);
		}

		public async Task DeleteAsync(CancellationToken cancellationToken = default)
		{
			var deleteQueueTask = _queue.DeleteIfExistsAsync(cancellationToken);
			var deleteBlobContainerTask = _blobContainer.DeleteIfExistsAsync(null, cancellationToken);
			Task.WaitAll(deleteQueueTask, deleteBlobContainerTask);
		}

		public async Task DeleteMessageAsync(CloudMessage message, CancellationToken cancellationToken = default)
		{
			var isLargeMessage = message.Metadata.TryGetValue(CloudMessage.LARGE_CONTENT_BLOB_NAME_METADATA, out string largeContentBlobName);

			if (isLargeMessage)
			{
				var blob = _blobContainer.GetBlobClient(largeContentBlobName);
				await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken).ConfigureAwait(false);
			}

			await _queue.DeleteMessageAsync(message.Id, message.PopReceipt, cancellationToken).ConfigureAwait(false);
		}

		public async Task<QueueProperties> GetPropertiesAsync(CancellationToken cancellationToken = default)
		{
			var response = await _queue.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
			return response.Value;
		}

		public async Task<CloudMessage> GetMessageAsync(TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
		{
			// Get the next message from the queue
			var response = await _queue.ReceiveMessagesAsync(1, visibilityTimeout, cancellationToken).ConfigureAwait(false);
			var cloudMessage = response.Value?.First();

			// Convert the Azure SDK message into a Picton message
			var message = await ConvertToPictonMessageAsync(cloudMessage, cancellationToken).ConfigureAwait(false);

			return message;
		}

		public async Task<IEnumerable<CloudMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > _queue.MaxPeekableMessages) throw new ArgumentOutOfRangeException(nameof(messageCount), $"cannot be greater than {_queue.MaxPeekableMessages}");

			// Get the messages from the queue
			var response = await _queue.ReceiveMessagesAsync(messageCount, visibilityTimeout, cancellationToken).ConfigureAwait(false);
			var cloudMessages = response.Value;

			// Convert the Azure SDK messages into Picton messages
			if (cloudMessages == null) return Enumerable.Empty<CloudMessage>();
			return await Task.WhenAll(from cloudMessage in cloudMessages select ConvertToPictonMessageAsync(cloudMessage, cancellationToken)).ConfigureAwait(false);
		}

		public async Task<IEnumerable<QueueSignedIdentifier>> GetAccessPolicyAsync(CancellationToken cancellationToken = default)
		{
			var response = await _queue.GetAccessPolicyAsync(cancellationToken).ConfigureAwait(false);
			return response.Value;
		}

		public async Task<CloudMessage> PeekMessageAsync(CancellationToken cancellationToken = default)
		{
			// Peek at the next message in the queue
			var response = await _queue.PeekMessagesAsync(1, cancellationToken).ConfigureAwait(false);
			var cloudMessage = response.Value?.First();

			// Convert the Azure SDK message into a Picton message
			var message = await ConvertToPictonMessageAsync(cloudMessage, cancellationToken).ConfigureAwait(false);

			return message;
		}

		public async Task<IEnumerable<CloudMessage>> PeekMessagesAsync(int messageCount, CancellationToken cancellationToken = default)
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > _queue.MaxPeekableMessages) throw new ArgumentOutOfRangeException(nameof(messageCount), $"cannot be greater than {_queue.MaxPeekableMessages}");

			// Peek at the messages from the queue
			var response = await _queue.PeekMessagesAsync(messageCount, cancellationToken).ConfigureAwait(false);
			var cloudMessages = response.Value;

			// Convert the Azure SDK messages into Picton messages
			if (cloudMessages == null) return Enumerable.Empty<CloudMessage>();
			return await Task.WhenAll(from cloudMessage in cloudMessages select ConvertToPictonMessageAsync(cloudMessage, cancellationToken)).ConfigureAwait(false);
		}

		public Task SetMetadataAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
		{
			return _queue.SetMetadataAsync(metadata, cancellationToken);
		}

		public Task SetAccessPolicyAsync(IEnumerable<QueueSignedIdentifier> permissions, CancellationToken cancellationToken = default)
		{
			return _queue.SetAccessPolicyAsync(permissions, cancellationToken);
		}

		/* For the time being, we don't support updating the content of a message due to complexity
			In order to support updating content we need to consider the following scenarios
				1) Previous content was smaller than max size and we are updating with content that is also smaller than max size. This is a trivial scenario. We simply need to update the content in the Azure queue.
				2) Previous content exceeded max size and we are updating with content that also exceeds max size. This is also a trivial scenario. We simply need to update the content in the blob.
				3) Previous content was smaller than max size and we are updating with content that exceeds max size. We need to save the new content in a blob, and the queue message must be updated with a 'LargeMessageEnvelope'.
				4) Previous content exceeded max size and we are updating with content smaller than max size. We need to delete the blob item and update the queue message.

			Determining if the new content exceeds max size or not is easy (see AddMessageAsync) but how can we determine if previous content exceeded the max size?

		public Task UpdateMessageAsync(CloudMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default)
		{
			var cloudMessage = new CloudQueueMessage(message.Id, message.PopReceipt);
			return _queue.UpdateMessageAsync(cloudMessage, visibilityTimeout, updateFields, options, operationContext, cancellationToken);
		}
		*/

		public Task UpdateMessageVisibilityTimeoutAsync(CloudMessage message, TimeSpan visibilityTimeout, CancellationToken cancellationToken = default)
		{
			return _queue.UpdateMessageAsync(message.Id, message.PopReceipt, null, visibilityTimeout, cancellationToken);
		}

		public async Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default)
		{
			var properties = await _queue.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
			return properties.Value.ApproximateMessagesCount;
		}

		#endregion

		#region PRIVATE METHODS

		// This method has to be synchronous because it's invoked from the constructors
		private void Init()
		{
			_blobContainer.CreateIfNotExists();
			_queue.CreateIfNotExists();
		}

		private async Task<MessageEnvelope> DeserializeAsync(string messageContent, CancellationToken cancellationToken)
		{
			try
			{
				var serializedContent = Convert.FromBase64String(messageContent);
				var code = MessagePackBinary.GetMessagePackType(serializedContent, 0);
				if (code == MessagePackType.Extension)
				{
					// The message was added to the queue using Picton's QueueManager.
					// Therefore we know exactly how to deserialize the content.
					var header = MessagePackBinary.ReadExtensionFormatHeader(serializedContent, 0, out var _);
					if (header.TypeCode == LZ4_MESSAGEPACK_SERIALIZATION || header.TypeCode == TYPELESS_MESSAGEPACK_SERIALIZATION)
					{
						var deserializedContent = LZ4MessagePackSerializer.Typeless.Deserialize(serializedContent);

						// If the serialized content exceeded the max Azure Storage size limit, it was saved in a blob
						if (deserializedContent.GetType() == typeof(LargeMessageEnvelope))
						{
							var largeEnvelope = (LargeMessageEnvelope)deserializedContent;
							var blob = _blobContainer.GetBlobClient(largeEnvelope.BlobName);

							// Get the content from blob item
							var blobContent = await blob.DownloadTextAsync(cancellationToken).ConfigureAwait(false);

							// Deserialize the binary content
							var messageEnvelope = await DeserializeAsync(blobContent, cancellationToken).ConfigureAwait(false);

							// Add the name of the blob item to metadata
							if (messageEnvelope.Metadata == null) messageEnvelope.Metadata = new Dictionary<string, string>();
							messageEnvelope.Metadata[CloudMessage.LARGE_CONTENT_BLOB_NAME_METADATA] = largeEnvelope.BlobName;

							// Return the envelope
							return messageEnvelope;
						}
						else if (deserializedContent.GetType() == typeof(MessageEnvelope))
						{
							return (MessageEnvelope)deserializedContent;
						}
						else
						{
							throw new Exception($"Picton is unable to deserialize a message of type '{deserializedContent.GetType()}'");
						}
					}
					else
					{
						throw new Exception($"Picton is unable to deserialize content using serialization method '{header.TypeCode}'");
					}
				}
				else
				{
					throw new Exception("Picton is unable to figure out how to deserialize this message.");
				}
			}
			catch (Exception e) when (!e.Message.StartsWith("Picton is unable"))
			{
				// The message was presumably added to the queue using the
				// CloudQueue class in Microsoft's Azure Storage nuget package.
				return new MessageEnvelope()
				{
					Content = messageContent,
					Metadata = new Dictionary<string, string>(),
					Version = typeof(QueueManager).GetTypeInfo().Assembly.GetName().Version
				};
			}
		}

		private static string Serialize<T>(T message, IDictionary<string, string> metadata)
		{
			var typeOfMessage = message.GetType();
			if (typeOfMessage == typeof(MessageEnvelope) || typeOfMessage == typeof(LargeMessageEnvelope))
			{
				var lz4SerializedMessage = LZ4MessagePackSerializer.Typeless.Serialize(message);
				var messageAsString = Convert.ToBase64String(lz4SerializedMessage);
				return messageAsString;
			}
			else
			{
				var envelope = new MessageEnvelope()
				{
					Content = message,
					Metadata = metadata,
					Version = typeof(QueueManager).GetTypeInfo().Assembly.GetName().Version
				};

				var lz4SerializedMessage = LZ4MessagePackSerializer.Typeless.Serialize(envelope);
				var messageAsString = Convert.ToBase64String(lz4SerializedMessage);
				return messageAsString;
			}
		}

		private async Task<CloudMessage> ConvertToPictonMessageAsync(QueueMessage cloudMessage, CancellationToken cancellationToken)
		{
			// We get a null value when the queue is empty
			if (cloudMessage == null) return null;

			// Deserialize the content of the cloud message
			var messageEnvelope = await DeserializeAsync(cloudMessage.MessageText, cancellationToken).ConfigureAwait(false);

			var message = new CloudMessage(messageEnvelope.Content)
			{
				DequeueCount = cloudMessage.DequeueCount,
				ExpiresOn = cloudMessage.ExpiresOn,
				Id = cloudMessage.MessageId,
				InsertedOn = cloudMessage.InsertedOn,
				NextVisibleOn = cloudMessage.NextVisibleOn,
				PopReceipt = cloudMessage.PopReceipt,
				Metadata = messageEnvelope.Metadata ?? new Dictionary<string, string>()
			};
			return message;
		}

		private async Task<CloudMessage> ConvertToPictonMessageAsync(PeekedMessage cloudMessage, CancellationToken cancellationToken)
		{
			// We get a null value when the queue is empty
			if (cloudMessage == null) return null;

			// Deserialize the content of the cloud message
			var messageEnvelope = await DeserializeAsync(cloudMessage.MessageText, cancellationToken).ConfigureAwait(false);

			var message = new CloudMessage(messageEnvelope.Content)
			{
				DequeueCount = cloudMessage.DequeueCount,
				ExpiresOn = cloudMessage.ExpiresOn,
				Id = cloudMessage.MessageId,
				InsertedOn = cloudMessage.InsertedOn,
				NextVisibleOn = null,
				PopReceipt = null,
				Metadata = messageEnvelope.Metadata ?? new Dictionary<string, string>()
			};
			return message;
		}

		#endregion
	}
}
