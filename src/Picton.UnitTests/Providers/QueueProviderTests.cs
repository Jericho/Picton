using Microsoft.VisualStudio.TestTools.UnitTesting;
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

namespace Picton.Providers.UnitTests
{
	[TestClass]
	public class QueueProviderTests
	{
		private static readonly string QUEUE_STORAGE_URL = "http://bogus:10001/devstoreaccount1/";
		private static readonly string BLOB_STORAGE_URL = "http://bogus:10002/devstoreaccount1/";

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Null_queueName_throws()
		{
			var storageAccount = new Mock<IStorageAccount>(MockBehavior.Strict);
			var queueProvider = new QueueProvider(null, storageAccount.Object);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Empty_queueName_throws()
		{
			var storageAccount = new Mock<IStorageAccount>(MockBehavior.Strict);
			var queueProvider = new QueueProvider("", storageAccount.Object);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Blank_queueName_throws()
		{
			var storageAccount = new Mock<IStorageAccount>(MockBehavior.Strict);
			var queueProvider = new QueueProvider(" ", storageAccount.Object);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Null_IStorageAccount_throws()
		{
			var storageAccount = (IStorageAccount)null;
			var queueProvider = new QueueProvider("myqueue", storageAccount);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Null_storageAccount_throws()
		{
			var storageAccount = (IStorageAccount)null;
			var queueProvider = new QueueProvider("myqueue", storageAccount);
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.AddMessageAsync("Hello world!").Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		//[TestMethod]
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
		//	var queueProvider = new QueueProvider(queueName, storageAccount.Object);
		//	queueProvider.AddMessageAsync(excessivelyLargeContent).Wait();


		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.ClearAsync().Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.CreateAsync().Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.CreateIfNotExistsAsync().Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.DeleteIfExistsAsync().Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.DeleteMessageAsync(message).Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.ExistsAsync().Result;


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBeTrue();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.FetchAttributesAsync().Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var message = new CloudMessage("Hello world", typeof(string));

			mockQueue
				.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(cloudMessage))
				.Verifiable();


			// Act
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.GetMessageAsync().Result;


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.Content.ShouldBe(message.Content);
			result.ContentType.ShouldBe(message.ContentType);
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.GetMessagesAsync(5).Result.ToArray();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBe(messages);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.GetMessagesAsync(messageCount).Result.ToArray();


			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.GetMessagesAsync(messageCount).Result.ToArray();


			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.GetPermissionsAsync().Result;


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBe(permissions);
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.PeekMessageAsync().Result;


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBe(message);
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.PeekMessagesAsync(5).Result.ToArray();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			result.ShouldBe(messages);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.PeekMessagesAsync(messageCount).Result.ToArray();


			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			var result = queueProvider.PeekMessagesAsync(messageCount).Result.ToArray();


			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.SetMetadataAsync().Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.SetPermissionsAsync(permissions).Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
		}

		[TestMethod]
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
			var queueProvider = new QueueProvider(queueName, storageAccount.Object);
			queueProvider.UpdateMessageAsync(message, visibilityTimeout, updateFields).Wait();


			// Assert
			mockQueue.Verify();
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
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

		private static Mock<IBlobClient> GetMockBlobClient(Mock<CloudBlobContainer> mockBlobContainer)
		{
			var mockBlobClient = new Mock<IBlobClient>(MockBehavior.Strict);
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

		private static Mock<IQueueClient> GetMockQueueClient(Mock<CloudQueue> mockQueue)
		{
			var mockQueueClient = new Mock<IQueueClient>(MockBehavior.Strict);
			mockQueueClient
				.Setup(c => c.GetQueueReference(mockQueue.Object.Name))
				.Returns(mockQueue.Object)
				.Verifiable();
			return mockQueueClient;
		}

		private static Mock<IStorageAccount> GetMockStorageAccount(Mock<IBlobClient> mockBlobClient, Mock<IQueueClient> mockQueueClient)
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
