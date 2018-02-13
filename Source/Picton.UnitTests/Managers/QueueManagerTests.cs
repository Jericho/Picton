using MessagePack;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Moq;
using Picton.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.UnitTests.Managers
{
	internal class SampleMessageType
	{
		public string StringProp { get; set; }
		public int IntProp { get; set; }
		public Guid GuidProp { get; set; }
		public DateTime DateProp { get; set; }
	}

	public class QueueMangerTests
	{
		private static readonly string QUEUE_STORAGE_URL = "http://bogus:10001/devstoreaccount1/";
		private static readonly string BLOB_STORAGE_URL = "http://bogus:10002/devstoreaccount1/";

		[Fact]
		public void Null_queueName_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = GetMockStorageAccount(null, null);
				var queueManager = new QueueManager(null, storageAccount.Object);
			});
		}

		[Fact]
		public void Empty_queueName_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = GetMockStorageAccount(null, null);
				var queueManager = new QueueManager("", storageAccount.Object);
			});
		}

		[Fact]
		public void Blank_queueName_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = GetMockStorageAccount(null, null);
				var queueManager = new QueueManager(" ", storageAccount.Object);
			});
		}

		[Fact]
		public void Null_StorageAccount_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = (CloudStorageAccount)null;
				var queueManager = new QueueManager("myqueue", storageAccount);
			});
		}

		[Fact]
		public void Null_storageAccount_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = (CloudStorageAccount)null;
				var queueManager = new QueueManager("myqueue", storageAccount);
			});
		}

		[Fact]
		public void Initialization()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void AddMessageAsync_small_message()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.AddMessageAsync("Hello world!").Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void AddMessageAsync_large_message_is_compressed()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var largeContentWillBeCompressed = new String('z', (int)CloudQueueMessage.MaxMessageSize * 2);
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.AddMessageAsync(largeContentWillBeCompressed).Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void ClearAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.ClearAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.ClearAsync().Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void CreateAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.CreateAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.CreateAsync().Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void CreateIfNotExistsAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.CreateIfNotExistsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.CreateIfNotExistsAsync().Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void DeleteIfExistsAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.DeleteIfExistsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.DeleteIfExistsAsync().Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void DeleteMessageAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var message = new CloudMessage("Hello world");

			mockQueue
				.Setup(c => c.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.DeleteMessageAsync(message).Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void ExistsAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.ExistsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.ExistsAsync().Result;

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBeTrue();
		}

		[Fact]
		public void FetchAttributesAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.FetchAttributesAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.FetchAttributesAsync().Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void GetMessageAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var cloudMessage = new CloudQueueMessage("Hello world");
			var message = new CloudMessage("Hello world");

			mockQueue
				.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(cloudMessage))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.GetMessageAsync().Result;

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.Content.GetType().ShouldBe(message.Content.GetType());
			result.Content.ShouldBe(message.Content);
		}

		[Fact]
		public void GetMessageAsync_when_queue_is_empty()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult((CloudQueueMessage)null))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.GetMessageAsync().Result;

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBeNull();
		}

		[Fact]
		// Serializing and deserializing an instance of an internal class didn't work in MessagePack version 1.7.0 until 1.7.3.
		// It was resolved in 1.7.3.1 (see: https://github.com/neuecc/MessagePack-CSharp/issues/187)
		// This unit test validates was used to demonstrate the issue.
		public void Serialize_Internal_Type()
		{
			var sampleMessage = new SampleMessageType
			{
				DateProp = new DateTime(2016, 10, 8, 1, 2, 3, DateTimeKind.Utc),
				GuidProp = Guid.NewGuid(),
				IntProp = 123,
				StringProp = "Hello World"
			};
			var serializedMessage = LZ4MessagePackSerializer.Typeless.Serialize(sampleMessage);
			var deserializedMessage = LZ4MessagePackSerializer.Typeless.Deserialize(serializedMessage);
		}

		[Fact]
		public void GetMessagesAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var cloudMessages = new[]
			{
				new CloudQueueMessage("Message 1"),
				new CloudQueueMessage("Message 2")
			};

			mockQueue
				.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((IEnumerable<CloudQueueMessage>)cloudMessages)
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.GetMessagesAsync(5).Result.ToArray();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.Length.ShouldBe(cloudMessages.Length);
		}

		[Fact]
		public void GetMessagesAsync_throws_when_messageCount_is_too_small()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var messageCount = 0;

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			{
				var result = await queueManager.GetMessagesAsync(messageCount).ConfigureAwait(false);
			});

			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[Fact]
		public void GetMessagesAsync_throws_when_messageCount_is_too_large()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var messageCount = CloudQueueMessage.MaxNumberOfMessagesToPeek + 1;

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			{
				var result = await queueManager.GetMessagesAsync(messageCount).ConfigureAwait(false);
			});

			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[Fact]
		public void GetPermissionsAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var permissions = new QueuePermissions();

			mockQueue
				.Setup(c => c.GetPermissionsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(permissions))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.GetPermissionsAsync().Result;

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBe(permissions);
		}

		[Fact]
		public void PeekMessageAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var cloudMessage = new CloudQueueMessage("Hello world");
			var message = new CloudMessage("Hello world");

			mockQueue
				.Setup(c => c.PeekMessageAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(cloudMessage))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.PeekMessageAsync().Result;

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.Content.GetType().ShouldBe(message.Content.GetType());
			result.Content.ShouldBe(message.Content);
		}

		[Fact]
		public void PeekMessagesAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var cloudMessages = new[]
			{
				new CloudQueueMessage("Message 1"),
				new CloudQueueMessage("Message 2")
			};

			mockQueue
				.Setup(c => c.PeekMessagesAsync(It.IsAny<int>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult((IEnumerable<CloudQueueMessage>)cloudMessages))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.PeekMessagesAsync(5).Result.ToArray();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.Length.ShouldBe(cloudMessages.Length);
		}

		[Fact]
		public void PeekMessagesAsync_throws_when_messageCount_is_too_small()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var messageCount = 0;

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			{
				var result = await queueManager.PeekMessagesAsync(messageCount).ConfigureAwait(false);
			});

			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[Fact]
		public void PeekMessagesAsync_throws_when_messageCount_is_too_large()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var messageCount = CloudQueueMessage.MaxNumberOfMessagesToPeek + 1;

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			{
				var result = await queueManager.PeekMessagesAsync(messageCount).ConfigureAwait(false);
			});

			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[Fact]
		public void SetMetadataAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

			mockQueue
				.Setup(c => c.SetMetadataAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.SetMetadataAsync().Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void SetPermissionsAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var permissions = new QueuePermissions();

			mockQueue
				.Setup(c => c.SetPermissionsAsync(It.IsAny<QueuePermissions>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.SetPermissionsAsync(permissions).Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		public void UpdateMessageVisibilityTimeoutAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var permissions = new QueuePermissions();
			var message = new CloudMessage("Hello world");
			var visibilityTimeout = TimeSpan.FromSeconds(2);

			mockQueue
				.Setup(c => c.UpdateMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan>(), MessageUpdateFields.Visibility, It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.UpdateMessageVisibilityTimeoutAsync(message, visibilityTimeout).Wait();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[Fact]
		// This unit test verifies that a message based on a POCO class can be serialized/deserialized.
		// This was not working in v1.3.0 and was fixed in v1.4.0
		public void Add_and_get_POCO_message()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var queuedMessage = (CloudQueueMessage)null;

			var sampleMessage = new SampleMessageType
			{
				DateProp = new DateTime(2016, 10, 8, 1, 2, 3, DateTimeKind.Utc),
				GuidProp = Guid.NewGuid(),
				IntProp = 123,
				StringProp = "Hello World"
			};

			mockQueue
				.Setup(c => c.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Callback((CloudQueueMessage m, TimeSpan? ttl, TimeSpan? v, QueueRequestOptions o, OperationContext c, CancellationToken t) =>
				{
					queuedMessage = m;
				})
				.Returns(Task.FromResult(true))
				.Verifiable();

			mockQueue
				.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(() => { return Task.FromResult(queuedMessage); })
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.AddMessageAsync(sampleMessage).Wait();
			var result = queueManager.GetMessageAsync().Result;

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();

			result.ShouldNotBeNull();
			result.Content.GetType().ShouldBe(typeof(SampleMessageType));

			var content = (SampleMessageType)result.Content;
			content.DateProp.ShouldBe(sampleMessage.DateProp);
			content.GuidProp.ShouldBe(sampleMessage.GuidProp);
			content.IntProp.ShouldBe(sampleMessage.IntProp);
			content.StringProp.ShouldBe(sampleMessage.StringProp);
		}

		[Fact]
		public void Add_and_get_large_message()
		{
			// Arrange
			var queueName = "myqueue";
			var mockBlobItemUri = new Uri(BLOB_STORAGE_URL + "test.txt");
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var queuedMessage = (CloudQueueMessage)null;
			var blobItemContent = (byte[])null;
			var excessivelyLargeContent = RandomGenerator.GenerateString((int)CloudQueueMessage.MaxMessageSize * 2);

			var mockBlobItem = new Mock<CloudBlockBlob>(MockBehavior.Strict, mockBlobItemUri);
			mockBlobItem
				.Setup(b => b.UploadFromStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Callback((Stream source, AccessCondition ac, BlobRequestOptions o, OperationContext c, CancellationToken t) =>
				{
					using (var ms = new MemoryStream())
					{
						source.CopyTo(ms);
						blobItemContent = ms.ToArray();
					}
				})
				.Returns(Task.FromResult(true))
				.Verifiable();

			mockBlobContainer
				.Setup(c => c.GetBlobReference(It.IsAny<string>()))
				.Returns(mockBlobItem.Object)
				.Verifiable();

			mockQueue
				.Setup(q => q.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Callback((CloudQueueMessage message, TimeSpan? ttl, TimeSpan? initialVisibilityDelay, QueueRequestOptions o, OperationContext c, CancellationToken t) =>
				{
					queuedMessage = message;
				})
				.Returns(Task.FromResult(true))
				.Verifiable();

			mockQueue
				.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(() => queuedMessage)
				.Verifiable();

			mockBlobItem
				.Setup(b => b.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Callback((Stream target, AccessCondition ac, BlobRequestOptions o, OperationContext c, CancellationToken t) =>
				{
					var bw = new BinaryWriter(target);
					try
					{
						bw.Write(blobItemContent);
						bw.Flush();
						target.Seek(0, SeekOrigin.Begin);
					}
					finally
					{
						bw.Dispose();
					}
				})
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.AddMessageAsync(excessivelyLargeContent).Wait();
			var result = queueManager.GetMessageAsync().Result;

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.Content.GetType().ShouldBe(typeof(string));
			result.Content.ShouldBe(excessivelyLargeContent);
		}

		private static Mock<CloudBlobContainer> GetMockBlobContainer(string containerName = "mycontainer")
		{
			var mockContainerUri = new Uri(BLOB_STORAGE_URL + containerName);
			var mockBlobContainer = new Mock<CloudBlobContainer>(MockBehavior.Strict, mockContainerUri);
			mockBlobContainer
				.Setup(c => c.CreateIfNotExistsAsync(It.IsAny<BlobContainerPublicAccessType>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(true)
				.Verifiable();
			return mockBlobContainer;
		}

		private static Mock<CloudBlobClient> GetMockBlobClient(Mock<CloudBlobContainer> mockBlobContainer)
		{
			var mockBlobStorageUri = new Uri(BLOB_STORAGE_URL);
			var mockBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, mockBlobStorageUri);
			mockBlobClient
				.Setup(c => c.GetContainerReference(It.IsAny<string>()))
				.Returns(mockBlobContainer.Object)
				.Verifiable();
			return mockBlobClient;
		}

		private static Mock<CloudQueue> GetMockQueue(string queueName)
		{
			var queueAddres = new Uri(QUEUE_STORAGE_URL + queueName);
			var mockQueue = new Mock<CloudQueue>(MockBehavior.Strict, queueAddres);
			mockQueue
				.Setup(c => c.CreateIfNotExistsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(false)
				.Verifiable();
			return mockQueue;
		}

		private static Mock<CloudQueueClient> GetMockQueueClient(Mock<CloudQueue> mockQueue)
		{
			var mockQueueStorageUri = new Uri(QUEUE_STORAGE_URL);
			var storageCredentials = GetStorageCredentials();
			var mockQueueClient = new Mock<CloudQueueClient>(MockBehavior.Strict, mockQueueStorageUri, storageCredentials);
			mockQueueClient
				.Setup(c => c.GetQueueReference(mockQueue.Object.Name))
				.Returns(mockQueue.Object)
				.Verifiable();
			return mockQueueClient;
		}

		private static Mock<CloudStorageAccount> GetMockStorageAccount(Mock<CloudBlobClient> mockBlobClient, Mock<CloudQueueClient> mockQueueClient)
		{
			var storageCredentials = GetStorageCredentials();
			var storageAccount = new Mock<CloudStorageAccount>(MockBehavior.Strict, storageCredentials, true);

			if (mockBlobClient != null)
			{
				storageAccount
					.Setup(s => s.CreateCloudBlobClient())
					.Returns(mockBlobClient.Object)
					.Verifiable();
			}

			if (mockQueueClient != null)
			{
				storageAccount
					.Setup(s => s.CreateCloudQueueClient())
					.Returns(mockQueueClient.Object)
					.Verifiable();
			}

			return storageAccount;
		}

		private static StorageCredentials GetStorageCredentials()
		{
			var accountAccessKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("this_is_a_bogus_account_access_key"));
			var storageCredentials = new StorageCredentials("account_name", accountAccessKey);
			return storageCredentials;
		}
	}
}
