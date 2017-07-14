using MessagePack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Picton.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	public class QueueManager : IQueueManager
	{
		#region FIELDS

		private static readonly long MAX_MESSAGE_CONTENT_SIZE = (CloudQueueMessage.MaxMessageSize - 1) / 4 * 3;

		private readonly IStorageAccount _storageAccount;
		private readonly string _queueName;
		private readonly CloudQueue _queue;
		private readonly CloudBlobContainer _blobContainer;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// </summary>
		/// <param name="queueName"></param>
		/// <param name="cloudStorageAccount"></param>
#if NETFULL
		[ExcludeFromCodeCoverage]
#endif
		public QueueManager(string queueName, CloudStorageAccount cloudStorageAccount)
			: this(queueName, StorageAccount.FromCloudStorageAccount(cloudStorageAccount))
		{
		}

		/// <summary>
		/// For unit testing
		/// </summary>
		/// <param name="queueName"></param>
		public QueueManager(string queueName, IStorageAccount storageAccount)
		{
			if (string.IsNullOrWhiteSpace(queueName)) throw new ArgumentNullException(nameof(queueName));
			if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));

			_storageAccount = storageAccount;
			_queueName = queueName;
			_queue = storageAccount.CreateCloudQueueClient().GetQueueReference(queueName);
			_blobContainer = storageAccount.CreateCloudBlobClient().GetContainerReference("oversizedqueuemessages");

			var tasks = new List<Task>();
			tasks.Add(_queue.CreateIfNotExistsAsync(null, null, CancellationToken.None));
			tasks.Add(_blobContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null, CancellationToken.None));
			Task.WaitAll(tasks.ToArray());
		}

		#endregion

		#region PUBLIC METHODS

		public async Task AddMessageAsync<T>(T message, TimeSpan? timeToLive = null, TimeSpan? initialVisibilityDelay = null, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var data = Serialize(message);

			// Check if the message exceeds the size allowed by Azure Storage queues
			if (data.Length > MAX_MESSAGE_CONTENT_SIZE)
			{
				// The message is too large. Therefore we must save the content to blob storage and
				// send a smaller message indicating where the actual message was saved

				// 1) Save the large message to blob storage
				var blobName = $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}-{RandomGenerator.GenerateString(32)}";
				var blob = _blobContainer.GetBlockBlobReference(blobName);
				await blob.UploadBytesAsync(data, null, cancellationToken).ConfigureAwait(false);

				// 2) Send a smaller message
				var largeEnvelope = new LargeMessageEnvelope
				{
					BlobName = blobName
				};
				data = Serialize(largeEnvelope);

				/*
					There is a constructor that accepts an array of bytes in NETFULL but it is not available in NETSTANDARD.
					The work around is to initialize with an empty string and subsequently invoke the 'SetMessageContent' method with the byte array
				*/
				var cloudMessage = new CloudQueueMessage(string.Empty);
				cloudMessage.SetMessageContent(data);
				await _queue.AddMessageAsync(cloudMessage, timeToLive, initialVisibilityDelay, options, operationContext, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				// The size of this message is within the range allowed by Azure Storage queues
				/*
					There is a constructor that accepts an array of bytes in NETFULL but it is not available in NETSTANDARD.
					The work around is to initialize with an empty string and subsequently invoke the 'SetMessageContent' method with the byte array
				*/
				var cloudMessage = new CloudQueueMessage(string.Empty);
				cloudMessage.SetMessageContent(data);
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
			if (message.IsLargeMessage)
			{
				var blob = _blobContainer.GetBlobReference(message.LargeContentBlobName);
				await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null, cancellationToken);
			}

			await _queue.DeleteMessageAsync(message.Id, message.PopReceipt, options, operationContext, cancellationToken);
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
			var message = await ConvertToPictonMessage(cloudMessage, cancellationToken).ConfigureAwait(false);

			return message;
		}

		public async Task<IEnumerable<CloudMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout = null, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > CloudQueueMessage.MaxNumberOfMessagesToPeek) throw new ArgumentOutOfRangeException(nameof(messageCount), $"cannot be greater than {CloudQueueMessage.MaxNumberOfMessagesToPeek}");

			// Get the messages from the queue
			var cloudMessages = await _queue.GetMessagesAsync(messageCount, visibilityTimeout, options, operationContext, cancellationToken).ConfigureAwait(false);

			// Convert the Azure SDK messages into Picton messages
			return await Task.WhenAll(from cloudMessage in cloudMessages select ConvertToPictonMessage(cloudMessage, cancellationToken)).ConfigureAwait(false);
		}

		public Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.GetPermissionsAsync(options, operationContext, cancellationToken);
		}

		// GetSharedAccessSignature is not virtual therefore we can't mock it.
#if NETFULL
		[ExcludeFromCodeCoverage]
#endif
		public string GetSharedAccessSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, SharedAccessProtocol? protocols = null, IPAddressOrRange ipAddressOrRange = null)
		{
			return _queue.GetSharedAccessSignature(policy, accessPolicyIdentifier, protocols, ipAddressOrRange);
		}

		public async Task<CloudMessage> PeekMessageAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			// Peek at the next message in the queue
			var cloudMessage = await _queue.PeekMessageAsync(options, operationContext, cancellationToken).ConfigureAwait(false);

			// Convert the Azure SDK message into a Picton message
			var message = await ConvertToPictonMessage(cloudMessage, cancellationToken).ConfigureAwait(false);

			return message;
		}

		public async Task<IEnumerable<CloudMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > CloudQueueMessage.MaxNumberOfMessagesToPeek) throw new ArgumentOutOfRangeException(nameof(messageCount), $"cannot be greather than {CloudQueueMessage.MaxNumberOfMessagesToPeek}");

			// Peek at the messages from the queue
			var cloudMessages = await _queue.PeekMessagesAsync(messageCount, options, operationContext, cancellationToken).ConfigureAwait(false);

			// Convert the Azure SDK messages into Picton messages
			return await Task.WhenAll(from cloudMessage in cloudMessages select ConvertToPictonMessage(cloudMessage, cancellationToken)).ConfigureAwait(false);
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

		#endregion

		#region PRIVATE METHODS

		private object Deserialize(byte[] serializedContent)
		{
			return MessagePackSerializer.Typeless.Deserialize(serializedContent);
		}

		private byte[] Serialize<T>(T message)
		{
			return MessagePackSerializer.Typeless.Serialize(message);
		}

		private async Task<CloudMessage> ConvertToPictonMessage(CloudQueueMessage cloudMessage, CancellationToken cancellationToken)
		{
			// We get a null value when the queue is empty
			if (cloudMessage == null)
			{
				return null;
			}

			// Deserialize the content of the cloud message
			var content = (object)null;
			try
			{
				content = cloudMessage.AsString;
			}
			catch
			{
				content = Deserialize(cloudMessage.AsBytes);
			}

			// If the serialized content exceeded the max Azure Storage size limit, it was saved in a blob
			var largeContentBlobName = (string)null;
			if (content.GetType() == typeof(LargeMessageEnvelope))
			{
				var envelope = (LargeMessageEnvelope)content;
				var blob = _blobContainer.GetBlobReference(envelope.BlobName);

				byte[] buffer;
				using (var ms = new MemoryStream())
				{
					await blob.DownloadToStreamAsync(ms, null, null, null, cancellationToken).ConfigureAwait(false);
					buffer = ms.ToArray();
				}

				content = Deserialize(buffer);
				largeContentBlobName = envelope.BlobName;
			}

			var message = new CloudMessage(content)
			{
				DequeueCount = cloudMessage.DequeueCount,
				ExpirationTime = cloudMessage.ExpirationTime,
				Id = cloudMessage.Id,
				InsertionTime = cloudMessage.InsertionTime,
				LargeContentBlobName = largeContentBlobName,
				NextVisibleTime = cloudMessage.NextVisibleTime,
				PopReceipt = cloudMessage.PopReceipt
			};
			return message;
		}

		#endregion
	}
}
