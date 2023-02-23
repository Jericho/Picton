using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using MessagePack;
using MessagePack.Resolvers;
using Moq;
using Picton.Interfaces;
using Picton.Managers;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
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
		[Fact]
		public void Null_blobContainerClient_throws()
		{
			Should.Throw<ArgumentNullException>(() =>
			{
				// Arrange
				var blobContainer = (BlobContainerClient)null;
				var mockQueueClient = MockUtils.GetMockQueueClient();

				// Act
				new QueueManager(blobContainer, mockQueueClient.Object, false);
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
				new QueueManager(mockBlobContainer.Object, queueClient, false);
			});
		}

		[Fact]
		public void Creates_container_and_queue_if_they_do_not_exist()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);

			// Act
			new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);

			// Assert
			mockBlobContainer.Verify();
			mockQueueClient.Verify();
		}

		[Fact]
		public async Task Small_message()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);

			mockQueueClient
				.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((string messageText, TimeSpan? visibilityTimeout, TimeSpan? timeToLive, CancellationToken cancellationToken) =>
				{
					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await queueManager.AddMessageAsync("Hello world!").ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
		}

		[Fact]
		public async Task Large_message_is_compressed()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);

			mockQueueClient
				.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((string messageText, TimeSpan? visibilityTimeout, TimeSpan? timeToLive, CancellationToken cancellationToken) =>
				{
					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				})
				  .Verifiable();

			// This string is highly "compressible" because it contains only one character repeated numerous times
			var largeContentWillBeCompressed = new String('z', (int)mockQueueClient.Object.MessageMaxBytes * 2);

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await queueManager.AddMessageAsync(largeContentWillBeCompressed).ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
		}

		[Fact]
		public async Task Clear_the_queue()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);

			mockQueueClient
				.Setup(q => q.ClearMessagesAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync((CancellationToken cancellationToken) =>
				{
					var mockResponse = new Mock<Response>();
					mockResponse.SetupGet(r => r.Status).Returns(200);
					return mockResponse.Object;
				})
				  .Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await queueManager.ClearAsync().ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
		}

		[Fact]
		public async Task DeleteIfExistsAsync()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);

			mockBlobContainer
				.Setup(c => c.DeleteIfExistsAsync(null, It.IsAny<CancellationToken>()))
				.ReturnsAsync((BlobRequestConditions conditions, CancellationToken cancellationToken) =>
				{
					return Response.FromValue(true, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			mockQueueClient
				.Setup(c => c.DeleteIfExistsAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync((CancellationToken cancellationToken) =>
				{
					return Response.FromValue(true, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await queueManager.DeleteAsync().ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
		}

		[Fact]
		public async Task DeleteMessageAsync()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var message = new CloudMessage("Hello world");

			mockQueueClient
				.Setup(c => c.DeleteMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((string messageId, string popReceipt, CancellationToken cancellationToken) =>
				{
					var mockResponse = new Mock<Response>();
					mockResponse.SetupGet(r => r.Status).Returns(200);
					return mockResponse.Object;
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await queueManager.DeleteMessageAsync(message).ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
		}

		[Fact]
		public async Task GetMessageAsync()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var messageContent = "Message 1";

			var LZ4Standard = MessagePackSerializerOptions.Standard
				.WithResolver(TypelessContractlessStandardResolver.Instance)
				.WithCompression(MessagePackCompression.Lz4Block);

			mockQueueClient
				.Setup(c => c.ReceiveMessagesAsync(1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((int? maxMessages, TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
				{
					var serializedMessage = QueueManager.SerializeMessage(messageContent, ImmutableDictionary<string, string>.Empty);
					var queueMessage = QueuesModelFactory.QueueMessage("myMessageId", "myPopReceipt", new BinaryData(serializedMessage), 0, null, null, null);

					return Response.FromValue(new[] { queueMessage }, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			var result = await queueManager.GetMessageAsync().ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			result.Content.GetType().ShouldBe(typeof(string));
			((string)result.Content).ShouldBe(messageContent);
		}

		[Fact]
		public async Task GetMessageAsync_when_queue_is_empty()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);

			mockQueueClient
				.Setup(c => c.ReceiveMessagesAsync(1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((int? maxMessages, TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
				{
					return Response.FromValue(Array.Empty<QueueMessage>(), new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			var result = await queueManager.GetMessageAsync().ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			result.ShouldBeNull();
		}

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

		[Fact]
		public async Task GetMessagesAsync()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var messageCount = 5;
			var messageContent1 = "Message 1";
			var messageContent2 = "Message 2";

			var LZ4Standard = MessagePackSerializerOptions.Standard
				.WithResolver(TypelessContractlessStandardResolver.Instance)
				.WithCompression(MessagePackCompression.Lz4Block);

			mockQueueClient
				.Setup(c => c.ReceiveMessagesAsync(messageCount, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((int? maxMessages, TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
				{
					var serializedMessage1 = QueueManager.SerializeMessage(messageContent1, ImmutableDictionary<string, string>.Empty);
					var queueMessage1 = QueuesModelFactory.QueueMessage("myMessageId1", "myPopReceipt1", new BinaryData(serializedMessage1), 0, null, null, null);

					var serializedMessage2 = QueueManager.SerializeMessage(messageContent2, ImmutableDictionary<string, string>.Empty);
					var queueMessage2 = QueuesModelFactory.QueueMessage("myMessageId2", "myPopReceipt2", new BinaryData(serializedMessage2), 0, null, null, null);

					return Response.FromValue(new[] { queueMessage1, queueMessage2 }, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			var result = await queueManager.GetMessagesAsync(messageCount).ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			result.Length.ShouldBe(2);
			result[0].Content.GetType().ShouldBe(typeof(string));
			((string)result[0].Content).ShouldBe(messageContent1);
			result[1].Content.GetType().ShouldBe(typeof(string));
			((string)result[1].Content).ShouldBe(messageContent2);
		}

		[Fact]
		public async Task GetMessagesAsync_throws_when_messageCount_is_too_small()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var messageCount = 0;

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => queueManager.GetMessagesAsync(messageCount)).ConfigureAwait(false);

			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[Fact]
		public async Task GetMessagesAsync_throws_when_messageCount_is_too_large()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var messageCount = mockQueueClient.Object.MaxPeekableMessages + 1;

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => queueManager.GetMessagesAsync(messageCount)).ConfigureAwait(false);

			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[Fact]
		public async Task PeekMessageAsync()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var messageContent = "Message 1";

			var LZ4Standard = MessagePackSerializerOptions.Standard
				.WithResolver(TypelessContractlessStandardResolver.Instance)
				.WithCompression(MessagePackCompression.Lz4Block);

			mockQueueClient
				.Setup(c => c.PeekMessagesAsync(1, It.IsAny<CancellationToken>()))
				.ReturnsAsync((int? maxMessages, CancellationToken cancellationToken) =>
				{
					var serializedMessage = QueueManager.SerializeMessage(messageContent, ImmutableDictionary<string, string>.Empty);
					var queueMessage = QueuesModelFactory.PeekedMessage("myMessageId", new BinaryData(serializedMessage), 0, null, null);

					return Response.FromValue(new[] { queueMessage }, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			var result = await queueManager.PeekMessageAsync().ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			result.Content.GetType().ShouldBe(typeof(string));
			((string)result.Content).ShouldBe(messageContent);
		}

		[Fact]
		public async Task PeekMessagesAsync()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var messageCount = 5;
			var messageContent1 = "Message 1";
			var messageContent2 = "Message 2";

			var LZ4Standard = MessagePackSerializerOptions.Standard
				.WithResolver(TypelessContractlessStandardResolver.Instance)
				.WithCompression(MessagePackCompression.Lz4Block);

			mockQueueClient
				.Setup(c => c.PeekMessagesAsync(messageCount, It.IsAny<CancellationToken>()))
				.ReturnsAsync((int? maxMessages, CancellationToken cancellationToken) =>
				{
					var serializedMessage1 = QueueManager.SerializeMessage(messageContent1, ImmutableDictionary<string, string>.Empty);
					var queueMessage1 = QueuesModelFactory.PeekedMessage("myMessageId1", new BinaryData(serializedMessage1), 0, null, null);

					var serializedMessage2 = QueueManager.SerializeMessage(messageContent2, ImmutableDictionary<string, string>.Empty);
					var queueMessage2 = QueuesModelFactory.PeekedMessage("myMessageId2", new BinaryData(serializedMessage2), 0, null, null);

					return Response.FromValue(new[] { queueMessage1, queueMessage2 }, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			var result = await queueManager.PeekMessagesAsync(messageCount).ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			result.Length.ShouldBe(2);
			result[0].Content.GetType().ShouldBe(typeof(string));
			((string)result[0].Content).ShouldBe(messageContent1);
			result[1].Content.GetType().ShouldBe(typeof(string));
			((string)result[1].Content).ShouldBe(messageContent2);
		}

		[Fact]
		public async Task PeekMessagesAsync_throws_when_messageCount_is_too_small()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var messageCount = 0;

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => queueManager.PeekMessagesAsync(messageCount)).ConfigureAwait(false);

			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[Fact]
		public async Task PeekMessagesAsync_throws_when_messageCount_is_too_large()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var messageCount = mockQueueClient.Object.MaxPeekableMessages + 1;

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => queueManager.PeekMessagesAsync(messageCount)).ConfigureAwait(false);

			// Assert
			// Nothing to assert because an exception will be thrown
		}

		[Fact]
		public async Task SetMetadataAsync()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var myMetadata = new Dictionary<string, string>()
			{
				{ "key1", "value1" },
				{ "key2", "value2" },
			};

			mockQueueClient
				.Setup(c => c.SetMetadataAsync(It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((IDictionary<string, string> metadata, CancellationToken cancellationToken) =>
				{
					var mockResponse = new Mock<Response>();
					mockResponse.SetupGet(r => r.Status).Returns(200);
					return mockResponse.Object;
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await queueManager.SetMetadataAsync(myMetadata).ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
		}

		[Fact]
		public async Task UpdateMessageVisibilityTimeoutAsync()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			var myMessage = new CloudMessage("Hello world");
			var timeout = TimeSpan.FromSeconds(2);

			mockQueueClient
				.Setup(q => q.UpdateMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<BinaryData>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((string messageId, string popReceipt, BinaryData message, TimeSpan visibilityTimeout, CancellationToken cancellationToken) =>
				{
					var receipt = QueuesModelFactory.UpdateReceipt("myPopReceipt", DateTimeOffset.UtcNow);
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				})
				  .Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await queueManager.UpdateMessageVisibilityTimeoutAsync(myMessage, timeout).ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
		}

		[Fact]
		// This unit test verifies that a message based on a POCO class can be serialized/deserialized.
		// This was not working in v1.3.0 and was fixed in v1.4.0
		public async Task Add_and_get_POCO_message()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, null);
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			string queuedContent = null;

			var sampleMessage = new SampleMessageType
			{
				DateProp = new DateTime(2016, 10, 8, 1, 2, 3, DateTimeKind.Utc),
				GuidProp = Guid.NewGuid(),
				IntProp = 123,
				StringProp = "Hello World"
			};

			mockQueueClient
				.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((string messageText, TimeSpan? visibilityTimeout, TimeSpan? timeToLive, CancellationToken cancellationToken) =>
				{
					queuedContent = messageText;
					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			mockQueueClient
				.Setup(c => c.ReceiveMessagesAsync(1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((int? maxMessages, TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
				{
					var queueMessage = QueuesModelFactory.QueueMessage("myMessageId", "myPopReceipt", new BinaryData(queuedContent), 0, null, null, null);
					return Response.FromValue(new[] { queueMessage }, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true);
			await queueManager.AddMessageAsync(sampleMessage).ConfigureAwait(false);
			var result = await queueManager.GetMessageAsync().ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();

			result.ShouldNotBeNull();
			result.Content.GetType().ShouldBe(typeof(SampleMessageType));

			var content = (SampleMessageType)result.Content;
			content.DateProp.ShouldBe(sampleMessage.DateProp);
			content.GuidProp.ShouldBe(sampleMessage.GuidProp);
			content.IntProp.ShouldBe(sampleMessage.IntProp);
			content.StringProp.ShouldBe(sampleMessage.StringProp);
		}

		[Fact]
		public async Task Add_and_get_large_message()
		{
			// Arrange
			var containerName = "mycontainer";
			var queueName = "myqueue";
			var systemClock = new MockSystemClock(new DateTime(2022, 4, 20, 10, 0, 0, 0, DateTimeKind.Utc)).Object;
			string queuedContent = null;
			byte[] blobItemContent = null;

			// Mock random generator that generates the name of the blob item
			var mockRandomGenerator = new Mock<IRandomGenerator>();
			mockRandomGenerator
				.Setup(r => r.GenerateString(It.IsAny<int>(), It.IsAny<string>()))
				.Returns("RandomString")
				.Verifiable();

			// The MockBlobClient simulates saving/retrieving the original mesage to/from blob
			var blobName = $"{systemClock.UtcNow:yyyy-MM-dd-HH-mm-ss}-{mockRandomGenerator.Object.GenerateString(32)}";
			var mockBlobClient = MockUtils.GetMockBlobClient(blobName);
			mockBlobClient
				.Setup(b => b.GetPropertiesAsync(It.IsAny<BlobRequestConditions>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((BlobRequestConditions conditions, CancellationToken cancellationToken) =>
				{
					var blobProperties = BlobsModelFactory.BlobProperties();
					return Response.FromValue(blobProperties, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();
			mockBlobClient
				.Setup(b => b.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobHttpHeaders>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<BlobRequestConditions>(), It.IsAny<IProgress<long>>(), It.IsAny<AccessTier?>(), It.IsAny<StorageTransferOptions>(), It.IsAny<CancellationToken>()))
				.Callback((Stream content, BlobHttpHeaders httpHeaders, IDictionary<string, string> metadata, BlobRequestConditions conditions, IProgress<long> progressHandler, AccessTier? accessTier, StorageTransferOptions transferOptions, CancellationToken cancellationToken) =>
				{
					using (var ms = new MemoryStream())
					{
						content.CopyTo(ms);
						blobItemContent = ms.ToArray();
					}
				})
				.ReturnsAsync((Stream content, BlobHttpHeaders httpHeaders, IDictionary<string, string> metadata, BlobRequestConditions conditions, IProgress<long> progressHandler, AccessTier? accessTier, StorageTransferOptions transferOptions, CancellationToken cancellationToken) =>
				{
					var blobContentInfo = BlobsModelFactory.BlobContentInfo(new ETag("mytag"), new DateTimeOffset(systemClock.UtcNow, TimeSpan.FromSeconds(0)), null, null, null, null, 0);
					return Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			mockBlobClient
				.Setup(b => b.DownloadAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync((CancellationToken cancellationToken) =>
				{
					var content = new MemoryStream();
					var bw = new BinaryWriter(content, Encoding.UTF8, true);
					try
					{
						bw.Write(blobItemContent);
						bw.Flush();
						content.Seek(0, SeekOrigin.Begin);
					}
					finally
					{
						bw.Dispose();
					}

					var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(
						lastModified: new DateTimeOffset(systemClock.UtcNow, TimeSpan.FromSeconds(0)),
						blobType: BlobType.Block,
						content: content);

					return Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok"));

				})
				.Verifiable();

			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, new[] { mockBlobClient });

			// MockQueueClient will send and receive the 'LargeMessageEnvelope' message that indicates the original message was saved to a blob
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			mockQueueClient
				.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<TimeSpan?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((string messageText, TimeSpan? visibilityTimeout, TimeSpan? timeToLive, CancellationToken cancellationToken) =>
				{
					queuedContent = messageText;
					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();
			mockQueueClient
				.Setup(c => c.ReceiveMessagesAsync(1, It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync((int? maxMessages, TimeSpan? visibilityTimeout, CancellationToken cancellationToken) =>
				{
					var queueMessage = QueuesModelFactory.QueueMessage("myMessageId", "myPopReceipt", new BinaryData(queuedContent), 0, null, null, null);
					return Response.FromValue(new[] { queueMessage }, new MockAzureResponse(200, "ok"));
				})
				.Verifiable();

			// We want to generate actual random content, so we use that default random generator
			// (i.e.: RandomGenerator.Instance) as opposed to the mock one created above
			var excessivelyLargeContent = RandomGenerator.Instance.GenerateString(mockQueueClient.Object.MessageMaxBytes * 2);

			// Act
			var queueManager = new QueueManager(mockBlobContainer.Object, mockQueueClient.Object, true, systemClock, mockRandomGenerator.Object);
			await queueManager.AddMessageAsync(excessivelyLargeContent).ConfigureAwait(false);
			var result = await queueManager.GetMessageAsync().ConfigureAwait(false);

			// Assert
			mockQueueClient.Verify();
			mockBlobContainer.Verify();
			result.Content.GetType().ShouldBe(typeof(string));
			result.Content.ShouldBe(excessivelyLargeContent);
		}
	}
}
