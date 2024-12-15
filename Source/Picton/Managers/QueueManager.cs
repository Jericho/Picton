using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using MessagePack;
using MessagePack.Resolvers;
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
	/// <inheritdoc/>
	public class QueueManager : IQueueManager
	{
		#region FIELDS

		private const sbyte LZ4_MESSAGEPACK_SERIALIZATION = 99;
		private const sbyte TYPELESS_MESSAGEPACK_SERIALIZATION = 100;

		private static readonly MessagePackSerializerOptions LZ4Standard = MessagePackSerializerOptions.Standard
			.WithResolver(TypelessContractlessStandardResolver.Instance)
			.WithCompression(MessagePackCompression.Lz4Block);

		private readonly QueueClient _queue;
		private readonly BlobContainerClient _blobContainer;
		private readonly ISystemClock _systemClock;
		private readonly IRandomGenerator _randomGenerator;

		#endregion

		#region PROPERTIES

		/// <inheritdoc/>
		public string QueueName { get => _queue.Name; }

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
		/// <param name="oversizeMessagesBlobStorageName">Name of the blob storage where messages that exceed the maximum size for a queue message are stored.</param>
		/// <param name="autoCreateResources">Create the queue and blob container if they do not already exist.</param>
		/// <param name="queueClientOptions">
		/// Optional client options that define the transport pipeline
		/// policies for authentication, retries, etc., that are applied to
		/// every request to the queue.
		/// </param>
		/// <param name="blobClientOptions">
		/// Optional client options that define the transport pipeline
		/// policies for authentication, retries, etc., that are applied to
		/// every request to the blob storage.
		/// </param>
		[ExcludeFromCodeCoverage]
		public QueueManager(string connectionString, string queueName, string oversizeMessagesBlobStorageName = null, bool autoCreateResources = true, QueueClientOptions queueClientOptions = null, BlobClientOptions blobClientOptions = null)
		{
			if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
			if (string.IsNullOrEmpty(queueName)) throw new ArgumentNullException(nameof(queueName));

			_blobContainer = new BlobContainerClient(connectionString, string.IsNullOrEmpty(oversizeMessagesBlobStorageName) ? $"{queueName}-oversize-messages" : oversizeMessagesBlobStorageName, blobClientOptions);
			_queue = new QueueClient(connectionString, queueName, queueClientOptions);
			_systemClock = SystemClock.Instance;
			_randomGenerator = RandomGenerator.Instance;

			if (autoCreateResources) Init();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueueManager"/> class.
		/// </summary>
		/// <param name="blobContainerClient">The blob container.</param>
		/// <param name="queueClient">The queue client.</param>
		/// <param name="autoCreateResources">Create the queue and blob container if they do not already exist.</param>
		public QueueManager(BlobContainerClient blobContainerClient, QueueClient queueClient, bool autoCreateResources = true)
			: this(blobContainerClient, queueClient, autoCreateResources, null, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="QueueManager"/> class.
		/// </summary>
		/// <param name="blobContainerClient">The blob container.</param>
		/// <param name="queueClient">The queue client.</param>
		/// <param name="autoCreateResources">Create the queue and blob container if they do not already exist.</param>
		/// <param name="systemClock">Allows dependency injection of a clock for unit tesing purposes. Feel free to ignore this parameter.</param>
		/// <param name="randomGenerator">Allows dependency injection of a random number generator for unit tesing purposes. Feel free to ignore this parameter.</param>
		[ExcludeFromCodeCoverage]
		internal QueueManager(BlobContainerClient blobContainerClient, QueueClient queueClient, bool autoCreateResources = true, ISystemClock systemClock = null, IRandomGenerator randomGenerator = null)
		{
			_blobContainer = blobContainerClient ?? throw new ArgumentNullException(nameof(blobContainerClient));
			_queue = queueClient ?? throw new ArgumentNullException(nameof(queueClient));
			_systemClock = systemClock ?? SystemClock.Instance;
			_randomGenerator = randomGenerator ?? RandomGenerator.Instance;

			if (autoCreateResources) Init();
		}

		#endregion

		#region PUBLIC METHODS

		/// <inheritdoc/>
		public async Task AddMessageAsync<T>(T message, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = null, TimeSpan? initialVisibilityDelay = null, CancellationToken cancellationToken = default)
		{
			if (message == null) return;

			var data = SerializeMessage(message, metadata);

			// Check if the message exceeds the size allowed by Azure Storage queues
			if (data.Length > _queue.MessageMaxBytes)
			{
				// The message is too large. Therefore we must save the content to blob storage and
				// send a smaller message indicating where the actual message was saved

				// 1) Save the large message to blob storage
				var blobName = $"{_systemClock.UtcNow:yyyy-MM-dd-HH-mm-ss}-{_randomGenerator.GenerateString(32)}";
				var blob = _blobContainer.GetBlobClient(blobName);
				await blob.UploadTextAsync(data, null, null, null, null, cancellationToken).ConfigureAwait(false);

				// 2) Send a smaller message
				var largeEnvelope = new LargeMessageEnvelope
				{
					BlobName = blobName,
					Version = typeof(QueueManager).GetTypeInfo().Assembly.GetName().Version
				};
				data = SerializeMessage(largeEnvelope, null);

				await _queue.SafeSendMessageAsync(data, initialVisibilityDelay, timeToLive, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				// The size of this message is within the range allowed by Azure Storage queues
				await _queue.SafeSendMessageAsync(data, initialVisibilityDelay, timeToLive, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc/>
		public Task ClearAsync(CancellationToken cancellationToken = default)
		{
			return _queue.ClearMessagesAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public Task DeleteResourcesAsync(CancellationToken cancellationToken = default)
		{
			var deleteQueueTask = _queue.DeleteIfExistsAsync(cancellationToken);
			var deleteBlobContainerTask = _blobContainer.DeleteIfExistsAsync(null, cancellationToken);
			return Task.WhenAll(deleteQueueTask, deleteBlobContainerTask);
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public async Task<QueueProperties> GetPropertiesAsync(CancellationToken cancellationToken = default)
		{
			var response = await _queue.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
			return response.Value;
		}

		/// <inheritdoc/>
		public async Task<CloudMessage[]> GetMessagesAsync(int maxMessages = 1, TimeSpan? visibilityTimeout = default, CancellationToken cancellationToken = default)
		{
			if (maxMessages < 1) throw new ArgumentOutOfRangeException(nameof(maxMessages), "must be greather than zero");
			if (maxMessages > _queue.MaxPeekableMessages) throw new ArgumentOutOfRangeException(nameof(maxMessages), $"cannot be greater than {_queue.MaxPeekableMessages}");

			// Get the messages from the queue
			var response = await _queue.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken).ConfigureAwait(false);
			var cloudMessages = response.Value;

			// Convert the Azure SDK messages into Picton messages
			if (cloudMessages == null) return Array.Empty<CloudMessage>();
			return await Task.WhenAll(from cloudMessage in cloudMessages select ConvertToPictonMessageAsync(cloudMessage, _blobContainer, cancellationToken)).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task<IEnumerable<QueueSignedIdentifier>> GetAccessPolicyAsync(CancellationToken cancellationToken = default)
		{
			var response = await _queue.GetAccessPolicyAsync(cancellationToken).ConfigureAwait(false);
			return response.Value;
		}

		/// <inheritdoc/>
		public async Task<CloudMessage[]> PeekMessagesAsync(int messageCount, CancellationToken cancellationToken = default)
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > _queue.MaxPeekableMessages) throw new ArgumentOutOfRangeException(nameof(messageCount), $"cannot be greater than {_queue.MaxPeekableMessages}");

			// Peek at the messages from the queue
			var response = await _queue.PeekMessagesAsync(messageCount, cancellationToken).ConfigureAwait(false);
			var cloudMessages = response.Value;

			// Convert the Azure SDK messages into Picton messages
			if (cloudMessages == null) return Array.Empty<CloudMessage>();
			return await Task.WhenAll(from cloudMessage in cloudMessages select ConvertToPictonMessageAsync(cloudMessage, _blobContainer, cancellationToken)).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public Task SetMetadataAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken = default)
		{
			return _queue.SetMetadataAsync(metadata, cancellationToken);
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public Task UpdateMessageVisibilityTimeoutAsync(CloudMessage message, TimeSpan visibilityTimeout, CancellationToken cancellationToken = default)
		{
			return _queue.UpdateMessageAsync(message.Id, message.PopReceipt, (BinaryData)null, visibilityTimeout, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default)
		{
			var properties = await _queue.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
			return properties.Value.ApproximateMessagesCount;
		}

		#endregion

		#region PRIVATE METHODS

		internal static async Task<MessageEnvelope> DeserializeMessageAsync(string messageContent, BlobContainerClient blobContainerClient, CancellationToken cancellationToken)
		{
			static bool CheckSerializationType(ReadOnlyMemory<byte> memory)
			{
				var reader = new MessagePackReader(memory);

				if (reader.NextMessagePackType != MessagePackType.Extension)
				{
					throw new Exception("Picton is unable to figure out how this message was serialized and how to deserialize it.");
				}

				var extensionHeader = reader.ReadExtensionFormatHeader();

				if (extensionHeader.TypeCode != LZ4_MESSAGEPACK_SERIALIZATION && extensionHeader.TypeCode != TYPELESS_MESSAGEPACK_SERIALIZATION)
				{
					throw new Exception($"Picton is unable to deserialize content using serialization method '{extensionHeader.TypeCode}'");
				}

				return true;
			}

			try
			{
				var serializedContent = Convert.FromBase64String(messageContent);

				// Perform sanity-check to ensure we are able to deserialize the content
				CheckSerializationType(serializedContent);

				var deserializedContent = MessagePackSerializer.Typeless.Deserialize(serializedContent, LZ4Standard, cancellationToken);

				// If the serialized content exceeded the max Azure Storage size limit, it was saved in a blob
				if (deserializedContent.GetType() == typeof(LargeMessageEnvelope))
				{
					var largeEnvelope = (LargeMessageEnvelope)deserializedContent;
					var blob = blobContainerClient.GetBlobClient(largeEnvelope.BlobName);

					// Get the content from blob item
					var blobContent = await blob.DownloadTextAsync(cancellationToken).ConfigureAwait(false);

					// Deserialize the binary content
					var messageEnvelope = await DeserializeMessageAsync(blobContent, blobContainerClient, cancellationToken).ConfigureAwait(false);

					// Add the name of the blob item to metadata
					messageEnvelope.Metadata ??= new Dictionary<string, string>();
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
			catch (Exception e) when (e.GetType().FullName.StartsWith("Moq."))
			{
				throw;
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

		internal static string SerializeMessage<T>(T message, IDictionary<string, string> metadata)
		{
			var typeOfMessage = message.GetType();
			if (typeOfMessage == typeof(MessageEnvelope) || typeOfMessage == typeof(LargeMessageEnvelope))
			{
				var lz4SerializedMessage = MessagePackSerializer.Typeless.Serialize(message, LZ4Standard);
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

				var lz4SerializedMessage = MessagePackSerializer.Typeless.Serialize(envelope, LZ4Standard);
				var messageAsString = Convert.ToBase64String(lz4SerializedMessage);
				return messageAsString;
			}
		}

		private static async Task<CloudMessage> ConvertToPictonMessageAsync(QueueMessage cloudMessage, BlobContainerClient blobContainerClient, CancellationToken cancellationToken)
		{
			// We get a null value when the queue is empty
			if (cloudMessage == null) return null;

			// Deserialize the content of the cloud message
			var messageEnvelope = await DeserializeMessageAsync(cloudMessage.MessageText, blobContainerClient, cancellationToken).ConfigureAwait(false);

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

		private static async Task<CloudMessage> ConvertToPictonMessageAsync(PeekedMessage cloudMessage, BlobContainerClient blobContainerClient, CancellationToken cancellationToken)
		{
			// We get a null value when the queue is empty
			if (cloudMessage == null) return null;

			// Deserialize the content of the cloud message
			var messageEnvelope = await DeserializeMessageAsync(cloudMessage.MessageText, blobContainerClient, cancellationToken).ConfigureAwait(false);

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

		// This method has to be synchronous because it's invoked from the constructors
		private void Init()
		{
			_blobContainer.CreateIfNotExists();
			_queue.CreateIfNotExists();
		}

		#endregion
	}
}
