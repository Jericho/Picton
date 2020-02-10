using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Picton.Managers;
using Shouldly;
using System;
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
		public class Constructor
		{
			[Fact]
			public void Null_blobContainerClient_throws()
			{
				Should.Throw<ArgumentNullException>(() =>
				{
					// Arrange
					var blobContainer = (BlobContainerClient)null;
					var mockQueueClient = MockUtils.GetMockQueueClient();

					// Act
					new QueueManager(blobContainer, mockQueueClient.Object);
				});
			}

			[Fact]
			public void Null_queueClient_throws()
			{
				Should.Throw<ArgumentNullException>(() =>
				{
					// Arrange
					var mockBlobContainer = MockUtils.GetMockBlobContainerClient();
					var queueClient = (QueueClient)null;

					// Act
					new QueueManager(mockBlobContainer.Object, queueClient);
				});
			}
			[Fact]
			public void Creates_container_and_queue_if_they_do_not_exist()
			{
				// Arrange
				var containerName = "mycontainer";
				var queueName = "myqueue";
				var queueAlreadyExists = false;

				var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
				var mockQueueClient = MockUtils.GetMockQueueClient(queueName, queueAlreadyExists);

				// Act
				new QueueManager(mockBlobContainer.Object, mockQueueClient.Object);

				// Assert
				mockBlobContainer.Verify();
				mockQueueClient.Verify();
			}
		}

		//[Fact]
		//public void AddMessageAsync_small_message()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.AddMessageAsync("Hello world!").Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void AddMessageAsync_large_message_is_compressed()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var largeContentWillBeCompressed = new String('z', (int)CloudQueueMessage.MaxMessageSize * 2);
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.AddMessageAsync(largeContentWillBeCompressed).Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void ClearAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.ClearAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.ClearAsync().Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void CreateAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.CreateAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.CreateAsync().Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void CreateIfNotExistsAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.CreateIfNotExistsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.CreateIfNotExistsAsync().Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void DeleteIfExistsAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.DeleteIfExistsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.DeleteIfExistsAsync().Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void DeleteMessageAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var message = new CloudMessage("Hello world");

		//	mockQueue
		//		.Setup(c => c.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.DeleteMessageAsync(message).Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void ExistsAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.ExistsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	var result = queueManager.ExistsAsync().Result;

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	result.ShouldBeTrue();
		//}

		//[Fact]
		//public void FetchAttributesAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.FetchAttributesAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.FetchAttributesAsync().Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void GetMessageAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var cloudMessage = new CloudQueueMessage("Hello world");
		//	var message = new CloudMessage("Hello world");

		//	mockQueue
		//		.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(cloudMessage))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	var result = queueManager.GetMessageAsync().Result;

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	result.Content.GetType().ShouldBe(message.Content.GetType());
		//	result.Content.ShouldBe(message.Content);
		//}

		//[Fact]
		//public void GetMessageAsync_when_queue_is_empty()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult((CloudQueueMessage)null))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	var result = queueManager.GetMessageAsync().Result;

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	result.ShouldBeNull();
		//}

		//[Fact]
		//// Serializing and deserializing an instance of an internal class didn't work in MessagePack version 1.7.0 until 1.7.3.
		//// It was resolved in 1.7.3.1 (see: https://github.com/neuecc/MessagePack-CSharp/issues/187)
		//// This unit test was used to demonstrate the issue.
		//public void Serialize_Internal_Type()
		//{
		//	var sampleMessage = new SampleMessageType
		//	{
		//		DateProp = new DateTime(2016, 10, 8, 1, 2, 3, DateTimeKind.Utc),
		//		GuidProp = Guid.NewGuid(),
		//		IntProp = 123,
		//		StringProp = "Hello World"
		//	};
		//	var serializedMessage = LZ4MessagePackSerializer.Typeless.Serialize(sampleMessage);
		//	var deserializedMessage = (SampleMessageType)LZ4MessagePackSerializer.Typeless.Deserialize(serializedMessage);

		//	deserializedMessage.DateProp.ShouldBe(sampleMessage.DateProp);
		//	deserializedMessage.GuidProp.ShouldBe(sampleMessage.GuidProp);
		//	deserializedMessage.IntProp.ShouldBe(sampleMessage.IntProp);
		//	deserializedMessage.StringProp.ShouldBe(sampleMessage.StringProp);
		//}

		//[Fact]
		//public void GetMessagesAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var cloudMessages = new[]
		//	{
		//		new CloudQueueMessage("Message 1"),
		//		new CloudQueueMessage("Message 2")
		//	};

		//	mockQueue
		//		.Setup(c => c.GetMessagesAsync(It.IsAny<int>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.ReturnsAsync((IEnumerable<CloudQueueMessage>)cloudMessages)
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	var result = queueManager.GetMessagesAsync(5).Result.ToArray();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	result.Length.ShouldBe(cloudMessages.Length);
		//}

		//[Fact]
		//public void GetMessagesAsync_throws_when_messageCount_is_too_small()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var messageCount = 0;

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
		//	{
		//		var result = await queueManager.GetMessagesAsync(messageCount).ConfigureAwait(false);
		//	});

		//	// Assert
		//	// Nothing to assert because an exception will be thrown
		//}

		//[Fact]
		//public void GetMessagesAsync_throws_when_messageCount_is_too_large()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var messageCount = CloudQueueMessage.MaxNumberOfMessagesToPeek + 1;

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
		//	{
		//		var result = await queueManager.GetMessagesAsync(messageCount).ConfigureAwait(false);
		//	});

		//	// Assert
		//	// Nothing to assert because an exception will be thrown
		//}

		//[Fact]
		//public void GetPermissionsAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var permissions = new QueuePermissions();

		//	mockQueue
		//		.Setup(c => c.GetPermissionsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(permissions))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	var result = queueManager.GetPermissionsAsync().Result;

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	result.ShouldBe(permissions);
		//}

		//[Fact]
		//public void PeekMessageAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var cloudMessage = new CloudQueueMessage("Hello world");
		//	var message = new CloudMessage("Hello world");

		//	mockQueue
		//		.Setup(c => c.PeekMessageAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(cloudMessage))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	var result = queueManager.PeekMessageAsync().Result;

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	result.Content.GetType().ShouldBe(message.Content.GetType());
		//	result.Content.ShouldBe(message.Content);
		//}

		//[Fact]
		//public void PeekMessagesAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var cloudMessages = new[]
		//	{
		//		new CloudQueueMessage("Message 1"),
		//		new CloudQueueMessage("Message 2")
		//	};

		//	mockQueue
		//		.Setup(c => c.PeekMessagesAsync(It.IsAny<int>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult((IEnumerable<CloudQueueMessage>)cloudMessages))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	var result = queueManager.PeekMessagesAsync(5).Result.ToArray();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	result.Length.ShouldBe(cloudMessages.Length);
		//}

		//[Fact]
		//public void PeekMessagesAsync_throws_when_messageCount_is_too_small()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var messageCount = 0;

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
		//	{
		//		var result = await queueManager.PeekMessagesAsync(messageCount).ConfigureAwait(false);
		//	});

		//	// Assert
		//	// Nothing to assert because an exception will be thrown
		//}

		//[Fact]
		//public void PeekMessagesAsync_throws_when_messageCount_is_too_large()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var messageCount = CloudQueueMessage.MaxNumberOfMessagesToPeek + 1;

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
		//	{
		//		var result = await queueManager.PeekMessagesAsync(messageCount).ConfigureAwait(false);
		//	});

		//	// Assert
		//	// Nothing to assert because an exception will be thrown
		//}

		//[Fact]
		//public void SetMetadataAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);

		//	mockQueue
		//		.Setup(c => c.SetMetadataAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.SetMetadataAsync().Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void SetPermissionsAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var permissions = new QueuePermissions();

		//	mockQueue
		//		.Setup(c => c.SetPermissionsAsync(It.IsAny<QueuePermissions>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.SetPermissionsAsync(permissions).Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//public void UpdateMessageVisibilityTimeoutAsync()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var permissions = new QueuePermissions();
		//	var message = new CloudMessage("Hello world");
		//	var visibilityTimeout = TimeSpan.FromSeconds(2);

		//	mockQueue
		//		.Setup(c => c.UpdateMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan>(), MessageUpdateFields.Visibility, It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.UpdateMessageVisibilityTimeoutAsync(message, visibilityTimeout).Wait();

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//}

		//[Fact]
		//// This unit test verifies that a message based on a POCO class can be serialized/deserialized.
		//// This was not working in v1.3.0 and was fixed in v1.4.0
		//public void Add_and_get_POCO_message()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var queuedMessage = (CloudQueueMessage)null;

		//	var sampleMessage = new SampleMessageType
		//	{
		//		DateProp = new DateTime(2016, 10, 8, 1, 2, 3, DateTimeKind.Utc),
		//		GuidProp = Guid.NewGuid(),
		//		IntProp = 123,
		//		StringProp = "Hello World"
		//	};

		//	mockQueue
		//		.Setup(c => c.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Callback((CloudQueueMessage m, TimeSpan? ttl, TimeSpan? v, QueueRequestOptions o, OperationContext c, CancellationToken t) =>
		//		{
		//			queuedMessage = m;
		//		})
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	mockQueue
		//		.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(() => { return Task.FromResult(queuedMessage); })
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.AddMessageAsync(sampleMessage).Wait();
		//	var result = queueManager.GetMessageAsync().Result;

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();

		//	result.ShouldNotBeNull();
		//	result.Content.GetType().ShouldBe(typeof(SampleMessageType));

		//	var content = (SampleMessageType)result.Content;
		//	content.DateProp.ShouldBe(sampleMessage.DateProp);
		//	content.GuidProp.ShouldBe(sampleMessage.GuidProp);
		//	content.IntProp.ShouldBe(sampleMessage.IntProp);
		//	content.StringProp.ShouldBe(sampleMessage.StringProp);
		//}

		//[Fact]
		//public void Add_and_get_large_message()
		//{
		//	// Arrange
		//	var queueName = "myqueue";
		//	var mockBlobItemUri = new Uri(Misc.BLOB_STORAGE_URL + "test.txt");
		//	var mockQueue = Misc.GetMockQueue(queueName);
		//	var mockQueueClient = Misc.GetMockQueueClient(mockQueue);
		//	var mockBlobContainer = Misc.GetMockBlobContainer();
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var queuedMessage = (CloudQueueMessage)null;
		//	var blobItemContent = (byte[])null;
		//	var excessivelyLargeContent = RandomGenerator.GenerateString((int)CloudQueueMessage.MaxMessageSize * 2);

		//	var mockBlobItem = new Mock<CloudBlockBlob>(MockBehavior.Strict, mockBlobItemUri);
		//	mockBlobItem
		//		.Setup(b => b.UploadFromStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Callback((Stream source, AccessCondition ac, BlobRequestOptions o, OperationContext c, CancellationToken t) =>
		//		{
		//			using (var ms = new MemoryStream())
		//			{
		//				source.CopyTo(ms);
		//				blobItemContent = ms.ToArray();
		//			}
		//		})
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	mockBlobContainer
		//		.Setup(c => c.GetBlobReference(It.IsAny<string>()))
		//		.Returns(mockBlobItem.Object)
		//		.Verifiable();

		//	mockQueue
		//		.Setup(q => q.AddMessageAsync(It.IsAny<CloudQueueMessage>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Callback((CloudQueueMessage message, TimeSpan? ttl, TimeSpan? initialVisibilityDelay, QueueRequestOptions o, OperationContext c, CancellationToken t) =>
		//		{
		//			queuedMessage = message;
		//		})
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	mockQueue
		//		.Setup(c => c.GetMessageAsync(It.IsAny<TimeSpan?>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.ReturnsAsync(() => queuedMessage)
		//		.Verifiable();

		//	mockBlobItem
		//		.Setup(b => b.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Callback((Stream target, AccessCondition ac, BlobRequestOptions o, OperationContext c, CancellationToken t) =>
		//		{
		//			var bw = new BinaryWriter(target);
		//			try
		//			{
		//				bw.Write(blobItemContent);
		//				bw.Flush();
		//				target.Seek(0, SeekOrigin.Begin);
		//			}
		//			finally
		//			{
		//				bw.Dispose();
		//			}
		//		})
		//		.Returns(Task.FromResult(true))
		//		.Verifiable();

		//	// Act
		//	var queueManager = new QueueManager(queueName, mockQueueClient.Object, mockBlobClient.Object);
		//	queueManager.AddMessageAsync(excessivelyLargeContent).Wait();
		//	var result = queueManager.GetMessageAsync().Result;

		//	// Assert
		//	mockQueue.Verify();
		//	mockQueueClient.Verify();
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	result.Content.GetType().ShouldBe(typeof(string));
		//	result.Content.ShouldBe(excessivelyLargeContent);
		//}
	}
}
