using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Moq;
using Shouldly;
using System;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.UnitTests
{
	[TestClass]
	public class QueueClientTests
	{
		private static readonly string BLOB_STORAGE_URL = "http://bogus:10000/devstoreaccount1/";

		[TestMethod]
		public void GetServicePropertiesAsync()
		{
			// Arrange
			var mockCloudQueueClient = new Mock<CloudQueueClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL), new StorageCredentials());
			var serviceProperties = new ServiceProperties
			{
				Cors = new CorsProperties(),
				DefaultServiceVersion = "aaa111",
				HourMetrics = new MetricsProperties("bbb222"),
				MinuteMetrics = new MetricsProperties("ccc333"),
				Logging = new LoggingProperties("ddd444")
			};

			mockCloudQueueClient
				.Setup(c => c.GetServicePropertiesAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(serviceProperties))
				.Verifiable();

			// Act
			var queueClient = new QueueClient(mockCloudQueueClient.Object);
			var result = queueClient.GetServicePropertiesAsync();
			result.Wait();

			// Assert
			mockCloudQueueClient.Verify();
			result.Result.ShouldBe(serviceProperties);
		}

		[TestMethod]
		public void GetServiceStatsAsync()
		{
			// Arrange
			var mockCloudQueueClient = new Mock<CloudQueueClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL), new StorageCredentials());
			var serviceStats = (ServiceStats)FormatterServices.GetUninitializedObject(typeof(ServiceStats)); //does not call ctor

			mockCloudQueueClient
				.Setup(c => c.GetServiceStatsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(serviceStats))
				.Verifiable();

			// Act
			var queueClient = new QueueClient(mockCloudQueueClient.Object);
			var result = queueClient.GetServiceStatsAsync();
			result.Wait();

			// Assert
			mockCloudQueueClient.Verify();
			result.Result.ShouldBe(serviceStats);
		}

		[TestMethod]
		public void ListQueues()
		{
			// Arrange
			var mockCloudQueueClient = new Mock<CloudQueueClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL), new StorageCredentials());
			var queues = new[]
			{
				new CloudQueue(new Uri(BLOB_STORAGE_URL + "queue1")),
				new CloudQueue(new Uri(BLOB_STORAGE_URL + "queue2")),
				new CloudQueue(new Uri(BLOB_STORAGE_URL + "queue3"))
			};

			mockCloudQueueClient
				.Setup(c => c.ListQueues(It.IsAny<string>(), It.IsAny<QueueListingDetails>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>()))
				.Returns(queues)
				.Verifiable();

			// Act
			var queueClient = new QueueClient(mockCloudQueueClient.Object);
			var result = queueClient.ListQueues();

			// Assert
			mockCloudQueueClient.Verify();
			result.ShouldBe(queues);
		}

		[TestMethod]
		public void ListQueuesSegmentedAsync()
		{
			// Arrange
			var mockCloudQueueClient = new Mock<CloudQueueClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL), new StorageCredentials());
			var segmentedResult = (QueueResultSegment)FormatterServices.GetUninitializedObject(typeof(QueueResultSegment));

			mockCloudQueueClient
				.Setup(c => c.ListQueuesSegmentedAsync(It.IsAny<string>(), It.IsAny<QueueListingDetails>(), It.IsAny<int?>(), It.IsAny<QueueContinuationToken>(), It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(segmentedResult))
				.Verifiable();

			// Act
			var queueClient = new QueueClient(mockCloudQueueClient.Object);
			var result = queueClient.ListQueuesSegmentedAsync();
			result.Wait();

			// Assert
			mockCloudQueueClient.Verify();
			result.Result.ShouldBe(segmentedResult);
		}

		[TestMethod]
		public void SetServicePropertiesAsync()
		{
			// Arrange
			var mockCloudQueueClient = new Mock<CloudQueueClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL), new StorageCredentials());
			var serviceProperties = new ServiceProperties
			{
				Cors = new CorsProperties(),
				DefaultServiceVersion = "aaa111",
				HourMetrics = new MetricsProperties("bbb222"),
				MinuteMetrics = new MetricsProperties("ccc333"),
				Logging = new LoggingProperties("ddd444")
			};

			mockCloudQueueClient
				.Setup(c => c.SetServicePropertiesAsync(serviceProperties, It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var queueClient = new QueueClient(mockCloudQueueClient.Object);
			var result = queueClient.SetServicePropertiesAsync(serviceProperties);
			result.Wait();

			// Assert
			mockCloudQueueClient.Verify();
		}
	}
}
