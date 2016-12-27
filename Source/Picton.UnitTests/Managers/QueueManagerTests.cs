using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Moq;
using Picton.Interfaces;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.Managers.UnitTests
{
	public class SampleMessageType
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
				var storageAccount = new Mock<IStorageAccount>(MockBehavior.Strict);
				var queueManager = new QueueManager(null, storageAccount.Object);
			});
		}

		[Fact]
		public void Empty_queueName_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = new Mock<IStorageAccount>(MockBehavior.Strict);
				var queueManager = new QueueManager("", storageAccount.Object);
			});
		}

		[Fact]
		public void Blank_queueName_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = new Mock<IStorageAccount>(MockBehavior.Strict);
				var queueManager = new QueueManager(" ", storageAccount.Object);
			});
		}

		[Fact]
		public void Null_IStorageAccount_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = (IStorageAccount)null;
				var queueManager = new QueueManager("myqueue", storageAccount);
			});
		}

		[Fact]
		public void Null_storageAccount_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				var storageAccount = (IStorageAccount)null;
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

		//[Fact]
		//public void AddMessageAsync_large_message()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = GetMockQueue(queueName);
		//	var mockQueueClient = GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = GetMockBlobContainer();
		//	var mockBlobClient = GetMockBlobClient(mockBlobContainer);
		//	var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);

		//	var mockBlobItem = new Mock<CloudBlockBlob>(MockBehavior.Strict);
		//	mockBlobItem
		//		.Setup(b => b.UploadTextAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	mockBlobContainer
		//		.Setup(c => c.GetBlockBlobReference(It.IsAny<string>()))
		//		.Returns(mockBlobItem.Object)
		//		.Verifiable();

		//	mockQueue
		//		.Setup(q => q.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var excessivelyLargeContent = new String('z', (int)CloudQueueMessage.MaxMessageSize * 2);
		//	var queueManager = new QueueManager(queueName, storageAccount.Object);
		//	queueManager.AddMessageAsync(excessivelyLargeContent).Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

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
		public void GetMessagesAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var messages = new[]
			{
				new CloudQueueMessage("Message 1"),
				new CloudQueueMessage("Message 2")
			};

			mockQueue
				.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult((IEnumerable<CloudQueueMessage>)messages))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.GetMessagesAsync(5).Result.ToArray();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBe(messages);
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
			Should.Throw<ArgumentOutOfRangeException>(() =>
			{
				var queueManager = new QueueManager(queueName, storageAccount.Object);
				var result = queueManager.GetMessagesAsync(messageCount).Result.ToArray();
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
			Should.Throw<ArgumentOutOfRangeException>(() =>
			{
				var queueManager = new QueueManager(queueName, storageAccount.Object);
				var result = queueManager.GetMessagesAsync(messageCount).Result.ToArray();
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
			var message = new CloudQueueMessage("Hello world");

			mockQueue
				.Setup(c => c.PeekMessageAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(message))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.PeekMessageAsync().Result;

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBe(message);
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
			var messages = new[]
			{
				new CloudQueueMessage("Message 1"),
				new CloudQueueMessage("Message 2")
			};

			mockQueue
				.Setup(c => c.PeekMessagesAsync(It.IsAny<int>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult((IEnumerable<CloudQueueMessage>)messages))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			var result = queueManager.PeekMessagesAsync(5).Result.ToArray();

			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBe(messages);
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
			Should.Throw<ArgumentOutOfRangeException>(() =>
			{
				var queueManager = new QueueManager(queueName, storageAccount.Object);
				var result = queueManager.PeekMessagesAsync(messageCount).Result.ToArray();
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
			Should.Throw<ArgumentOutOfRangeException>(() =>
			{
				var queueManager = new QueueManager(queueName, storageAccount.Object);
				var result = queueManager.PeekMessagesAsync(messageCount).Result.ToArray();
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
		public void UpdateMessageAsync()
		{
			// Arrange
			var queueName = "myqueue";
			var mockQueue = GetMockQueue(queueName);
			var mockQueueClient = GetMockQueueClient(mockQueue);
			var mockBlobContainer = GetMockBlobContainer();
			var mockBlobClient = GetMockBlobClient(mockBlobContainer);
			var storageAccount = GetMockStorageAccount(mockBlobClient, mockQueueClient);
			var permissions = new QueuePermissions();
			var message = new CloudQueueMessage("Hello world");
			var visibilityTimeout = TimeSpan.FromSeconds(2);
			var updateFields = MessageUpdateFields.Visibility;

			mockQueue
				.Setup(c => c.UpdateMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan>(), It.IsAny<MessageUpdateFields>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueManager = new QueueManager(queueName, storageAccount.Object);
			queueManager.UpdateMessageAsync(message, visibilityTimeout, updateFields).Wait();

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
			var mockBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict);
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
			var mockQueueClient = new Mock<CloudQueueClient>(MockBehavior.Strict);
			mockQueueClient
				.Setup(c => c.GetQueueReference(mockQueue.Object.Name))
				.Returns(mockQueue.Object)
				.Verifiable();
			return mockQueueClient;
		}

		private static Mock<IStorageAccount> GetMockStorageAccount(Mock<CloudBlobClient> mockBlobClient, Mock<CloudQueueClient> mockQueueClient)
		{
			var storageAccount = new Mock<IStorageAccount>(MockBehavior.Strict);
			storageAccount
				.Setup(s => s.CreateCloudBlobClient())
				.Returns(mockBlobClient.Object)
				.Verifiable();
			storageAccount
				.Setup(s => s.CreateCloudQueueClient())
				.Returns(mockQueueClient.Object)
				.Verifiable();
			return storageAccount;
		}
	}
}
