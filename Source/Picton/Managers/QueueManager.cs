using MessagePack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Picton.Interfaces;
using Picton.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	public class QueueManager : IQueueManager
	{
		#region FIELDS

		private const sbyte LZ4_MESSAGEPACK_SERIALIZATION = 99;
		private const sbyte TYPELESS_MESSAGEPACK_SERIALIZATION = 100;

		private static readonly long MAX_MESSAGE_CONTENT_SIZE = (CloudQueueMessage.MaxMessageSize - 1) / 4 * 3;
		private static readonly UTF8Encoding UTF8_ENCODER = new UTF8Encoding(false, true);

		private readonly string _queueName;
		private readonly CloudQueue _queue;
		private readonly CloudBlobContainer _blobContainer;

		#endregion

		#region PROPERTIES

		public IDictionary<string, string> Metadata
		{
			get
			{
				return _queue.Metadata;
			}
		}

		#endregion

		#region CONSTRUCTORS

		// This constructor must be excluded from code covereage because CreateCloudQueueClient and
		// CreateCloudBlobClient are extension methods since Microsoft.Azure.Storage.Blob 9.4 and
		// extension methods cannot be mocked.
		[ExcludeFromCodeCoverage]
		public QueueManager(string queueName, CloudStorageAccount storageAccount)
		{
			if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));

			var queueClient = storageAccount.CreateCloudQueueClient();
			var blobClient = storageAccount.CreateCloudBlobClient();

			_queueName = !string.IsNullOrWhiteSpace(queueName) ? queueName : throw new ArgumentNullException(nameof(queueName));
			_queue = queueClient.GetQueueReference(queueName);
			_blobContainer = blobClient.GetContainerReference("oversizedqueuemessages");

			InitQueueManager();
		}

		public QueueManager(string queueName, CloudQueueClient queueClient, CloudBlobClient blobClient)
		{
			if (queueClient == null) throw new ArgumentNullException(nameof(queueClient));
			if (blobClient == null) throw new ArgumentNullException(nameof(blobClient));

			_queueName = !string.IsNullOrWhiteSpace(queueName) ? queueName : throw new ArgumentNullException(nameof(queueName));
			_queue = queueClient.GetQueueReference(queueName);
			_blobContainer = blobClient.GetContainerReference("oversizedqueuemessages");

			InitQueueManager();
		}

		#endregion

		#region PUBLIC METHODS

		public async Task AddMessageAsync<T>(T message, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = null, TimeSpan? initialVisibilityDelay = null, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (message == null) return;

			var data = Serialize(message, metadata);

			// Check if the message exceeds the size allowed by Azure Storage queues
			if (data.Length > MAX_MESSAGE_CONTENT_SIZE)
			{
				// The message is too large. Therefore we must save the content to blob storage and
				// send a smaller message indicating where the actual message was saved

				// 1) Save the large message to blob storage
				var blobName = $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}-{RandomGenerator.GenerateString(32)}";
				var blob = _blobContainer.GetBlobReference(blobName);
				await blob.UploadBytesAsync(data, null, cancellationToken).ConfigureAwait(false);

				// 2) Send a smaller message
				var largeEnvelope = new LargeMessageEnvelope
				{
					BlobName = blobName
				};
				data = Serialize(largeEnvelope, null);

				var cloudMessage = new CloudQueueMessage(data);
				await _queue.AddMessageAsync(cloudMessage, timeToLive, initialVisibilityDelay, options, operationContext, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				// The size of this message is within the range allowed by Azure Storage queues
				var cloudMessage = new CloudQueueMessage(data);
				await _queue.AddMessageAsync(cloudMessage, timeToLive, initialVisibilityDelay, options, operationContext, cancellationToken).ConfigureAwait(false);
			}
		}

		public Task ClearAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.ClearAsync(options, operationContext, cancellationToken);
		}

		public Task CreateAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.CreateAsync(options, operationContext, cancellationToken);
		}

		public Task<bool> CreateIfNotExistsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.CreateIfNotExistsAsync(options, operationContext, cancellationToken);
		}

		public Task<bool> DeleteIfExistsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.DeleteIfExistsAsync(options, operationContext, cancellationToken);
		}

		public async Task DeleteMessageAsync(CloudMessage message, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var isLargeMessage = message.Metadata.TryGetValue(CloudMessage.LARGE_CONTENT_BLOB_NAME_METADATA, out string largeContentBlobName);

			if (isLargeMessage)
			{
				var blob = _blobContainer.GetBlobReference(largeContentBlobName);
				await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null, cancellationToken).ConfigureAwait(false);
			}

			await _queue.DeleteMessageAsync(message.Id, message.PopReceipt, options, operationContext, cancellationToken).ConfigureAwait(false);
		}

		public Task<bool> ExistsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.ExistsAsync(options, operationContext, cancellationToken);
		}

		public Task FetchAttributesAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.FetchAttributesAsync(options, operationContext, cancellationToken);
		}

		public async Task<CloudMessage> GetMessageAsync(TimeSpan? visibilityTimeout = null, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			// Get the next message from the queue
			var cloudMessage = await _queue.GetMessageAsync(visibilityTimeout, options, operationContext, cancellationToken).ConfigureAwait(false);

			// Convert the Azure SDK message into a Picton message
			var message = await ConvertToPictonMessageAsync(cloudMessage, cancellationToken).ConfigureAwait(false);

			return message;
		}

		public async Task<IEnumerable<CloudMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout = null, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > CloudQueueMessage.MaxNumberOfMessagesToPeek) throw new ArgumentOutOfRangeException(nameof(messageCount), $"cannot be greater than {CloudQueueMessage.MaxNumberOfMessagesToPeek}");

			// Get the messages from the queue
			var cloudMessages = await _queue.GetMessagesAsync(messageCount, visibilityTimeout, options, operationContext, cancellationToken).ConfigureAwait(false);

			// Convert the Azure SDK messages into Picton messages
			if (cloudMessages == null) return Enumerable.Empty<CloudMessage>();
			return await Task.WhenAll(from cloudMessage in cloudMessages select ConvertToPictonMessageAsync(cloudMessage, cancellationToken)).ConfigureAwait(false);
		}

		public Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.GetPermissionsAsync(options, operationContext, cancellationToken);
		}

		// GetSharedAccessSignature is not virtual therefore we can't mock it.
		[ExcludeFromCodeCoverage]
		public string GetSharedAccessSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, SharedAccessProtocol? protocols = null, IPAddressOrRange ipAddressOrRange = null)
		{
			return _queue.GetSharedAccessSignature(policy, accessPolicyIdentifier, protocols, ipAddressOrRange);
		}

		public async Task<CloudMessage> PeekMessageAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			// Peek at the next message in the queue
			var cloudMessage = await _queue.PeekMessageAsync(options, operationContext, cancellationToken).ConfigureAwait(false);

			// Convert the Azure SDK message into a Picton message
			var message = await ConvertToPictonMessageAsync(cloudMessage, cancellationToken).ConfigureAwait(false);

			return message;
		}

		public async Task<IEnumerable<CloudMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > CloudQueueMessage.MaxNumberOfMessagesToPeek) throw new ArgumentOutOfRangeException(nameof(messageCount), $"cannot be greather than {CloudQueueMessage.MaxNumberOfMessagesToPeek}");

			// Peek at the messages from the queue
			var cloudMessages = await _queue.PeekMessagesAsync(messageCount, options, operationContext, cancellationToken).ConfigureAwait(false);

			// Convert the Azure SDK messages into Picton messages
			return await Task.WhenAll(from cloudMessage in cloudMessages select ConvertToPictonMessageAsync(cloudMessage, cancellationToken)).ConfigureAwait(false);
		}

		public Task SetMetadataAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.SetMetadataAsync(options, operationContext, cancellationToken);
		}

		public Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.SetPermissionsAsync(permissions, options, operationContext, cancellationToken);
		}

		/* For the time being, we don't support updating the content of a message due to complexity
			In order to support updating content we need to consider the following scenarios
				1) Previous content was smaller than max size and we are updating with content that is also smaller than max size. This is a trivial scenario. We simply need to update the content in the Azure queue.
				2) Previous content exceeded max size and we are updating with content that also exceeds max size. This is also a trivial scenario. We simply need to update the content in the blob.
				3) Previous content was smaller than max size and we are updating with content that exceeds max size. We need to save the new content in a blob, and the queue message must be updated with a 'LargeMessageEnvelope'
				4) Previous content exceeded max size and we are updating with content smaller than max size. We need to delete the blob item and update the queue message.

			Determining if the new content exceeds max size or not is easy (see AddMessageAsync) but how can we determine if previous content exceeded the max size?

		public Task UpdateMessageAsync(CloudMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cloudMessage = new CloudQueueMessage(message.Id, message.PopReceipt);
			return _queue.UpdateMessageAsync(cloudMessage, visibilityTimeout, updateFields, options, operationContext, cancellationToken);
		}
		*/

		public Task UpdateMessageVisibilityTimeoutAsync(CloudMessage message, TimeSpan visibilityTimeout, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cloudMessage = new CloudQueueMessage(message.Id, message.PopReceipt);
			return _queue.UpdateMessageAsync(cloudMessage, visibilityTimeout, MessageUpdateFields.Visibility, options, operationContext, cancellationToken);
		}

		public async Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default(CancellationToken))
		{
			await _queue.FetchAttributesAsync(null, null, cancellationToken).ConfigureAwait(false);
			return _queue.ApproximateMessageCount ?? 0;
		}

		#endregion

		#region PRIVATE METHODS

		private void InitQueueManager()
		{
			var tasks = new List<Task>()
			{
				_queue.CreateIfNotExistsAsync(null, null, CancellationToken.None),
				_blobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null, CancellationToken.None)
			};
			Task.WaitAll(tasks.ToArray());
		}

		private async Task<MessageEnvelope> DeserializeAsync(byte[] serializedContent, CancellationToken cancellationToken)
		{
			var code = MessagePackBinary.GetMessagePackType(serializedContent, 0);
			if (code == MessagePackType.Extension)
			{
				// The message was added to the queue using Picton's QueueManager.
				// Therefore we know exactly how to deserialize the content.
				var header = MessagePackBinary.ReadExtensionFormatHeader(serializedContent, 0, out var readSize);
				if (header.TypeCode == LZ4_MESSAGEPACK_SERIALIZATION || header.TypeCode == TYPELESS_MESSAGEPACK_SERIALIZATION)
				{
					var deserializedContent = LZ4MessagePackSerializer.Typeless.Deserialize(serializedContent);

					// If the serialized content exceeded the max Azure Storage size limit, it was saved in a blob
					if (deserializedContent.GetType() == typeof(LargeMessageEnvelope))
					{
						var largeEnvelope = (LargeMessageEnvelope)deserializedContent;
						var blob = _blobContainer.GetBlobReference(largeEnvelope.BlobName);

						// Get the binary content from blob item
						byte[] buffer;
						using (var ms = new MemoryStream())
						{
							await blob.DownloadToStreamAsync(ms, null, null, null, cancellationToken).ConfigureAwait(false);
							buffer = ms.ToArray();
						}

						// Deserialize the binary content
						var messageEnvelope = await DeserializeAsync(buffer, cancellationToken).ConfigureAwait(false);

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
				// The message was added to the queue using the CloudQueue class in Microsoft's Azure Storage nuget package
				// Therefore we can't be sure if the content is a string or binary.
				try
				{
					return new MessageEnvelope()
					{
						Content = UTF8_ENCODER.GetString(serializedContent, 0, serializedContent.Length),
						Metadata = new Dictionary<string, string>()
					};
				}
				catch
				{
					return new MessageEnvelope()
					{
						Content = serializedContent,
						Metadata = new Dictionary<string, string>()
					};
				}
			}
		}

		private static byte[] Serialize<T>(T message, IDictionary<string, string> metadata)
		{
			var typeOfMessage = message.GetType();
			if (typeOfMessage == typeof(MessageEnvelope) || typeOfMessage == typeof(LargeMessageEnvelope))
			{
				return LZ4MessagePackSerializer.Typeless.Serialize(message);
			}
			else
			{
				var envelope = new MessageEnvelope()
				{
					Content = message,
					Metadata = metadata
				};

				return LZ4MessagePackSerializer.Typeless.Serialize(envelope);
			}
		}

		private async Task<CloudMessage> ConvertToPictonMessageAsync(CloudQueueMessage cloudMessage, CancellationToken cancellationToken)
		{
			// We get a null value when the queue is empty
			if (cloudMessage == null)
			{
				return null;
			}

			// Deserialize the content of the cloud message
			var messageEnvelope = await DeserializeAsync(cloudMessage.AsBytes, cancellationToken).ConfigureAwait(false);

			var message = new CloudMessage(messageEnvelope.Content)
			{
				DequeueCount = cloudMessage.DequeueCount,
				ExpirationTime = cloudMessage.ExpirationTime,
				Id = cloudMessage.Id,
				InsertionTime = cloudMessage.InsertionTime,
				NextVisibleTime = cloudMessage.NextVisibleTime,
				PopReceipt = cloudMessage.PopReceipt,
				Metadata = messageEnvelope.Metadata ?? new Dictionary<string, string>()
			};
			return message;
		}

		#endregion
	}
}
