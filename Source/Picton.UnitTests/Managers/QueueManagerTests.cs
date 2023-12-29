using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using MessagePack;
using MessagePack.Resolvers;
using NSubstitute;
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
				new QueueManager(blobContainer, mockQueueClient, false);
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
				new QueueManager(mockBlobContainer, queueClient, false);
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
			new QueueManager(mockBlobContainer, mockQueueClient, true);

			// Assert
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
				.SendMessageAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var messageText = callInfo.ArgAt<string>(0);
					var visibilityTimeout = callInfo.ArgAt<TimeSpan?>(1);
					var timeToLive = callInfo.ArgAt<TimeSpan?>(2);
					var cancellationToken = callInfo.ArgAt<CancellationToken>(3);

					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await queueManager.AddMessageAsync("Hello world!");

			// Assert
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
				.SendMessageAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				});

			// This string is highly "compressible" because it contains only one character repeated numerous times
			var largeContentWillBeCompressed = new String('z', (int)mockQueueClient.MessageMaxBytes * 2);

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await queueManager.AddMessageAsync(largeContentWillBeCompressed);

			// Assert
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
				.ClearMessagesAsync(Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var mockResponse = Substitute.For<Response>();
					mockResponse.Status.Returns(200);
					return mockResponse;
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await queueManager.ClearAsync();

			// Assert
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
				.DeleteIfExistsAsync(null, Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					return Response.FromValue(true, new MockAzureResponse(200, "ok"));
				});

			mockQueueClient
				.DeleteIfExistsAsync(Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					return Response.FromValue(true, new MockAzureResponse(200, "ok"));
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await queueManager.DeleteAsync();

			// Assert
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
				.DeleteMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var mockResponse = Substitute.For<Response>();
					mockResponse.Status.Returns(200);
					return mockResponse;
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await queueManager.DeleteMessageAsync(message);

			// Assert
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
				.ReceiveMessagesAsync(1, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var maxMessages = callInfo.ArgAt<int?>(0);
					var visibilityTimeout = callInfo.ArgAt<TimeSpan?>(1);

					var serializedMessage = QueueManager.SerializeMessage(messageContent, ImmutableDictionary<string, string>.Empty);
					var queueMessage = QueuesModelFactory.QueueMessage("myMessageId", "myPopReceipt", new BinaryData(serializedMessage), 0, null, null, null);

					return Response.FromValue(new[] { queueMessage }, new MockAzureResponse(200, "ok"));
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			var result = await queueManager.GetMessageAsync();

			// Assert
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
				.ReceiveMessagesAsync(1, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(Response.FromValue(Array.Empty<QueueMessage>(), new MockAzureResponse(200, "ok")));

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			var result = await queueManager.GetMessageAsync();

			// Assert
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
				.ReceiveMessagesAsync(messageCount, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var serializedMessage1 = QueueManager.SerializeMessage(messageContent1, ImmutableDictionary<string, string>.Empty);
					var queueMessage1 = QueuesModelFactory.QueueMessage("myMessageId1", "myPopReceipt1", new BinaryData(serializedMessage1), 0, null, null, null);

					var serializedMessage2 = QueueManager.SerializeMessage(messageContent2, ImmutableDictionary<string, string>.Empty);
					var queueMessage2 = QueuesModelFactory.QueueMessage("myMessageId2", "myPopReceipt2", new BinaryData(serializedMessage2), 0, null, null, null);

					return Response.FromValue(new[] { queueMessage1, queueMessage2 }, new MockAzureResponse(200, "ok"));
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			var result = await queueManager.GetMessagesAsync(messageCount);

			// Assert
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
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => queueManager.GetMessagesAsync(messageCount));

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
			var messageCount = mockQueueClient.MaxPeekableMessages + 1;

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => queueManager.GetMessagesAsync(messageCount));

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
				.PeekMessagesAsync(1, Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var serializedMessage = QueueManager.SerializeMessage(messageContent, ImmutableDictionary<string, string>.Empty);
					var queueMessage = QueuesModelFactory.PeekedMessage("myMessageId", new BinaryData(serializedMessage), 0, null, null);

					return Response.FromValue(new[] { queueMessage }, new MockAzureResponse(200, "ok"));
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			var result = await queueManager.PeekMessageAsync();

			// Assert
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
				.PeekMessagesAsync(messageCount, Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var serializedMessage1 = QueueManager.SerializeMessage(messageContent1, ImmutableDictionary<string, string>.Empty);
					var queueMessage1 = QueuesModelFactory.PeekedMessage("myMessageId1", new BinaryData(serializedMessage1), 0, null, null);

					var serializedMessage2 = QueueManager.SerializeMessage(messageContent2, ImmutableDictionary<string, string>.Empty);
					var queueMessage2 = QueuesModelFactory.PeekedMessage("myMessageId2", new BinaryData(serializedMessage2), 0, null, null);

					return Response.FromValue(new[] { queueMessage1, queueMessage2 }, new MockAzureResponse(200, "ok"));
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			var result = await queueManager.PeekMessagesAsync(messageCount);

			// Assert
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
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => queueManager.PeekMessagesAsync(messageCount));

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
			var messageCount = mockQueueClient.MaxPeekableMessages + 1;

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => queueManager.PeekMessagesAsync(messageCount));

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
				.SetMetadataAsync(Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var mockResponse = Substitute.For<Response>();
					mockResponse.Status.Returns(200);
					return mockResponse;
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await queueManager.SetMetadataAsync(myMetadata);

			// Assert
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
				.UpdateMessageAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<BinaryData>(), Arg.Any<TimeSpan>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var receipt = QueuesModelFactory.UpdateReceipt("myPopReceipt", DateTimeOffset.UtcNow);
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await queueManager.UpdateMessageVisibilityTimeoutAsync(myMessage, timeout);

			// Assert
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
				.SendMessageAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					queuedContent = callInfo.ArgAt<string>(0);
					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				});

			mockQueueClient
				.ReceiveMessagesAsync(1, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var queueMessage = QueuesModelFactory.QueueMessage("myMessageId", "myPopReceipt", new BinaryData(queuedContent), 0, null, null, null);
					return Response.FromValue(new[] { queueMessage }, new MockAzureResponse(200, "ok"));
				});

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true);
			await queueManager.AddMessageAsync(sampleMessage);
			var result = await queueManager.GetMessageAsync();

			// Assert
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
			var systemClock = new MockSystemClock(2022, 4, 20, 10, 0, 0, 0);
			string queuedContent = null;
			byte[] blobItemContent = null;

			// Mock random generator that generates the name of the blob item
			var mockRandomGenerator = Substitute.For<IRandomGenerator>();
			mockRandomGenerator
				.GenerateString(Arg.Any<int>(), Arg.Any<string>())
				.Returns("RandomString");

			// The MockBlobClient simulates saving/retrieving the original mesage to/from blob
			var blobName = $"{systemClock.UtcNow:yyyy-MM-dd-HH-mm-ss}-{mockRandomGenerator.GenerateString(32)}";
			var mockBlobClient = MockUtils.GetMockBlobClient(blobName);
			mockBlobClient
				.GetPropertiesAsync(Arg.Any<BlobRequestConditions>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var blobProperties = BlobsModelFactory.BlobProperties();
					return Response.FromValue(blobProperties, new MockAzureResponse(200, "ok"));
				});
			mockBlobClient
				.UploadAsync(Arg.Do<Stream>(stream =>
				{
					using (var ms = new MemoryStream())
					{
						stream.CopyTo(ms);
						blobItemContent = ms.ToArray();
					}

				}), Arg.Any<BlobHttpHeaders>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<BlobRequestConditions>(), Arg.Any<IProgress<long>>(), Arg.Any<AccessTier?>(), Arg.Any<StorageTransferOptions>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var blobContentInfo = BlobsModelFactory.BlobContentInfo(new ETag("mytag"), new DateTimeOffset(systemClock.UtcNow, TimeSpan.FromSeconds(0)), null, null, null, null, 0);
					return Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok"));
				});

			mockBlobClient
				.DownloadAsync(Arg.Any<CancellationToken>())
				.Returns(callInfo =>
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

				});

			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, new[] { mockBlobClient });

			// MockQueueClient will send and receive the 'LargeMessageEnvelope' message that indicates the original message was saved to a blob
			var mockQueueClient = MockUtils.GetMockQueueClient(queueName);
			mockQueueClient
				.SendMessageAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					queuedContent = callInfo.ArgAt<string>(0);
					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				});
			mockQueueClient
				.ReceiveMessagesAsync(1, Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var queueMessage = QueuesModelFactory.QueueMessage("myMessageId", "myPopReceipt", new BinaryData(queuedContent), 0, null, null, null);
					return Response.FromValue(new[] { queueMessage }, new MockAzureResponse(200, "ok"));
				});

			// We want to generate actual random content, so we use the default random generator
			// (i.e.: RandomGenerator.Instance) as opposed to the mock one created above
			var excessivelyLargeContent = RandomGenerator.Instance.GenerateString(mockQueueClient.MessageMaxBytes * 2);

			// Act
			var queueManager = new QueueManager(mockBlobContainer, mockQueueClient, true, systemClock, mockRandomGenerator);
			await queueManager.AddMessageAsync(excessivelyLargeContent);
			var result = await queueManager.GetMessageAsync();

			// Assert
			result.Content.GetType().ShouldBe(typeof(string));
			result.Content.ShouldBe(excessivelyLargeContent);
		}
	}
}
