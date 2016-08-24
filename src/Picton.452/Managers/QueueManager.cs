using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Newtonsoft.Json;
using Picton.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	public class QueueManager : IQueueProvider
	{
		#region FIELDS

		private readonly IStorageAccount _storageAccount;
		private readonly string _queueName;
		private readonly CloudQueue _queue;
		private readonly CloudBlobContainer _blobContainer;
		private static readonly long MAX_MESSAGE_CONTENT_SIZE = ((CloudQueueMessage.MaxMessageSize - 1) / 4 * 3);
		private const string DateFormatInBlobName = "yyyy-MM-dd-HH-mm-ss-ffff";

		#endregion

		#region CONSTRUCTORS

		[ExcludeFromCodeCoverage]
		/// <summary>
		/// </summary>
		/// <param name="queueName"></param>
		/// <param name="cloudStorageAccount"></param>
		public QueueManager(string queueName, CloudStorageAccount cloudStorageAccount) :
			this(queueName, StorageAccount.FromCloudStorageAccount(cloudStorageAccount))
		{ }

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
			var envelope = new MessageEnvelope
			{
				Payload = message,
				PayloadType = message.GetType()
			};
			var serializedEnvelope = JsonConvert.SerializeObject(envelope);

			// Check if the message exceeds the size allowed by Azure Storage queues
			if (serializedEnvelope.Length > MAX_MESSAGE_CONTENT_SIZE)
			{
				// The message is too large. Therefore we must save the content to blob storage and
				// send a smaller message indicating where the actual message was saved

				// 1) Save the large message to blob storage
				var blobName = "abc123";
				var blob = _blobContainer.GetBlockBlobReference(blobName);
				await blob.UploadTextAsync(serializedEnvelope, cancellationToken).ConfigureAwait(false);

				// 2) Send a smaller message
				var largeEnvelope = new LargeMessageEnvelope
				{
					BlobName = blobName
				};
				var serializedLargeEnvelope = JsonConvert.SerializeObject(largeEnvelope);
				var cloudMessage = new CloudQueueMessage(serializedLargeEnvelope);
				await _queue.AddMessageAsync(cloudMessage, timeToLive, initialVisibilityDelay, options, operationContext, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				// The size of this message is within the range allowed by Azure Storage queues
				var cloudMessage = new CloudQueueMessage(serializedEnvelope);
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
			if (!string.IsNullOrEmpty(message.LargeContentBlobName))
			{
				var blob = _blobContainer.GetBlobReference(message.LargeContentBlobName);
				await blob.DeleteAsync(cancellationToken);
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
			var cloudMessage = await _queue.GetMessageAsync(visibilityTimeout, options, operationContext, cancellationToken).ConfigureAwait(false);
			var content = (object)null;
			var contentType = (Type)null;
			var largeContentBlobName = (string)null;

			// We don't know the type of the message but we can make educated guesses:
			// 1) If the message was added to a queue by invoking QueueProvider.AddMessage, the type is MessageContentEnvelope
			// 2) If the message exceeded the Azure Storage size limit, the type is MessageLargeContentEnvelope
			// 3) Otherwise, it was added to the queue using some other method (for example, using the Azure SDK or invoking the Azure REST API)
			//		and therfore we treat the content as a string.

			try
			{
				var envelope = JsonConvert.DeserializeObject<MessageEnvelope>(cloudMessage.AsString);
				content = envelope.Payload;
				contentType = envelope.PayloadType;
			}
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
			catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body

			if (content == null)
			{
				try
				{
					var largeEnvelope = JsonConvert.DeserializeObject<LargeMessageEnvelope>(cloudMessage.AsString);
					var blob = _blobContainer.GetBlobReference(largeEnvelope.BlobName);

					using (var stream = new MemoryStream())
					{
						await blob.DownloadToStreamAsync(stream);
						var serializer = new JsonSerializer();
						using (var streamReader = new StreamReader(stream))
						{
							var envelope = (MessageEnvelope)serializer.Deserialize(streamReader, typeof(MessageEnvelope));
							content = envelope.Payload;
							contentType = envelope.PayloadType;
							largeContentBlobName = largeEnvelope.BlobName;
						}
					}
				}
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
				catch { }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
			}

			if (content == null)
			{
				content = cloudMessage.AsString;
				contentType = typeof(string);
			}

			var message = new CloudMessage(content, contentType)
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

		public Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout = null, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > CloudQueueMessage.MaxNumberOfMessagesToPeek) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be less than or equal to {CloudQueueMessage.MaxNumberOfMessagesToPeek}");

			return _queue.GetMessagesAsync(messageCount, visibilityTimeout, options, operationContext, cancellationToken);

			//var cloudMessages = await _queue.GetMessagesAsync(messageCount, visibilityTimeout, options, operationContext, cancellationToken);
			//var messages = cloudMessages.Select(cloudMessage => _serializer.Deserialize(cloudMessage.AsBytes) as IMessage);
			//return messages;
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

		public Task<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.PeekMessageAsync(options, operationContext, cancellationToken);

			//var cloudMessage = await _queue.PeekMessageAsync(options, operationContext, cancellationToken);
			//var message = _serializer.Deserialize(cloudMessage.AsBytes);
			//return message as IMessage;
		}

		public Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (messageCount < 1) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be greather than zero");
			if (messageCount > CloudQueueMessage.MaxNumberOfMessagesToPeek) throw new ArgumentOutOfRangeException(nameof(messageCount), "must be less than or equal to {CloudQueueMessage.MaxNumberOfMessagesToPeek}");

			return _queue.PeekMessagesAsync(messageCount, options, operationContext, cancellationToken);

			//var cloudMessages = await _queue.PeekMessagesAsync(messageCount, options, operationContext, cancellationToken);
			//var messages = cloudMessages.Select(cloudMessage => _serializer.Deserialize(cloudMessage.AsBytes) as IMessage);
			//return messages;
		}

		public Task SetMetadataAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.SetMetadataAsync(options, operationContext, cancellationToken);
		}

		public Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.SetPermissionsAsync(permissions, options, operationContext, cancellationToken);
		}

		public Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _queue.UpdateMessageAsync(message, visibilityTimeout, updateFields, options, operationContext, cancellationToken);
		}

		#endregion
	}
}
