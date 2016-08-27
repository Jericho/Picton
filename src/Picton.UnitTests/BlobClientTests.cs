using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.UnitTests
{
	[TestClass]
	public class BlobClientTests
	{
		private static readonly string BLOB_STORAGE_URL = "http://bogus:10000/devstoreaccount1/";

		[TestMethod]
		public void GetBlobReferenceFromServerAsync()
		{
			// Arrange
			var mockCloudBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL));
			var blobUri = new StorageUri(new Uri(BLOB_STORAGE_URL + "blablabla.txt"));
			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);

			mockCloudBlobClient
				.Setup(c => c.GetBlobReferenceFromServerAsync(blobUri, It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(mockBlob.Object))
				.Verifiable();

			// Act
			var blobClient = new BlobClient(mockCloudBlobClient.Object);
			var result = blobClient.GetBlobReferenceFromServerAsync(blobUri);
			result.Wait();

			// Assert
			mockCloudBlobClient.Verify();
			mockBlob.Verify();
		}

		[TestMethod]
		public void GetServicePropertiesAsync()
		{
			// Arrange
			var mockCloudBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL));
			var serviceProperties = new ServiceProperties();

			mockCloudBlobClient
				.Setup(c => c.GetServicePropertiesAsync(It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(serviceProperties))
				.Verifiable();

			// Act
			var blobClient = new BlobClient(mockCloudBlobClient.Object);
			var result = blobClient.GetServicePropertiesAsync();
			result.Wait();

			// Assert
			mockCloudBlobClient.Verify();
			Assert.AreEqual(serviceProperties, result.Result);
		}

		[TestMethod]
		public void GetServiceStatsAsync()
		{
			// Arrange
			var mockCloudBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL));
			var serviceStats = (ServiceStats)FormatterServices.GetUninitializedObject(typeof(ServiceStats)); //does not call ctor

			mockCloudBlobClient
				.Setup(c => c.GetServiceStatsAsync(It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(serviceStats))
				.Verifiable();

			// Act
			var blobClient = new BlobClient(mockCloudBlobClient.Object);
			var result = blobClient.GetServiceStatsAsync();
			result.Wait();

			// Assert
			mockCloudBlobClient.Verify();
			result.Result.ShouldBe(serviceStats);
		}

		[TestMethod]
		public void ListBlobs()
		{
			// Arrange
			var mockCloudBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL));
			var prefix = "/myfiles/*.*";
			var useFlatBlobListing = true;
			var blobs = new[]
			{
				new CloudBlockBlob(new Uri(BLOB_STORAGE_URL + "test1.txt")),
				new CloudBlockBlob(new Uri(BLOB_STORAGE_URL + "test2.txt")),
				new CloudBlockBlob(new Uri(BLOB_STORAGE_URL + "test3.txt"))
			};

			mockCloudBlobClient
				.Setup(c => c.ListBlobs(prefix, useFlatBlobListing, It.IsAny<BlobListingDetails>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>()))
				.Returns(blobs)
				.Verifiable();

			// Act
			var blobClient = new BlobClient(mockCloudBlobClient.Object);
			var result = blobClient.ListBlobs(prefix, useFlatBlobListing);

			// Assert
			mockCloudBlobClient.Verify();
			CollectionAssert.AreEqual(blobs, result.ToArray());
		}

		[TestMethod]
		public void ListBlobsSegmentedAsync()
		{
			// Arrange
			var mockCloudBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL));
			var prefix = "/myfiles/*.*";
			var useFlatBlobListing = true;
			var segmentedResult = (BlobResultSegment)FormatterServices.GetUninitializedObject(typeof(BlobResultSegment));

			mockCloudBlobClient
				.Setup(c => c.ListBlobsSegmentedAsync(prefix, useFlatBlobListing, It.IsAny<BlobListingDetails>(), It.IsAny<int?>(), It.IsAny<BlobContinuationToken>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(segmentedResult))
				.Verifiable();

			// Act
			var blobClient = new BlobClient(mockCloudBlobClient.Object);
			var result = blobClient.ListBlobsSegmentedAsync(prefix, useFlatBlobListing);
			result.Wait();

			// Assert
			mockCloudBlobClient.Verify();
			result.Result.ShouldBe(segmentedResult);
		}

		[TestMethod]
		public void ListContainers()
		{
			// Arrange
			var mockCloudBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, new Uri("http://bogus/myaccount/blob"));
			var prefix = "";
			var containers = new[]
			{
				new CloudBlobContainer(new Uri("http://bogus/myaccount/blob/myfiles1")),
				new CloudBlobContainer(new Uri("http://bogus/myaccount/blob/myfiles2")),
				new CloudBlobContainer(new Uri("http://bogus/myaccount/blob/myfiles3"))
			};

			mockCloudBlobClient
				.Setup(c => c.ListContainers(prefix, It.IsAny<ContainerListingDetails>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>()))
				.Returns(containers)
				.Verifiable();

			// Act
			var blobClient = new BlobClient(mockCloudBlobClient.Object);
			var result = blobClient.ListContainers(prefix, ContainerListingDetails.All);

			// Assert
			mockCloudBlobClient.Verify();
			CollectionAssert.AreEqual(containers, result.ToArray());
		}

		[TestMethod]
		public void ListContainersSegmentedAsync()
		{
			// Arrange
			var mockCloudBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, new Uri(BLOB_STORAGE_URL));
			var segmentedResult = (ContainerResultSegment)FormatterServices.GetUninitializedObject(typeof(ContainerResultSegment));

			mockCloudBlobClient
				.Setup(c => c.ListContainersSegmentedAsync(It.IsAny<string>(), It.IsAny<ContainerListingDetails>(), It.IsAny<int?>(), It.IsAny<BlobContinuationToken>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(segmentedResult))
				.Verifiable();

			// Act
			var blobClient = new BlobClient(mockCloudBlobClient.Object);
			var result = blobClient.ListContainersSegmentedAsync();
			result.Wait();

			// Assert
			mockCloudBlobClient.Verify();
			result.Result.ShouldBe(segmentedResult);
		}

		[TestMethod]
		public void SetServicePropertiesAsync()
		{
			// Arrange
			var mockCloudBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, new Uri("http://bogus/myaccount/blob"));
			var properties = new ServiceProperties
			{
				Cors = new CorsProperties(),
				DefaultServiceVersion = "abc123",
				HourMetrics = new MetricsProperties("abc123"),
				MinuteMetrics = new MetricsProperties("def456"),
				Logging = new LoggingProperties("ghi789")
			};

			mockCloudBlobClient
				.Setup(c => c.SetServicePropertiesAsync(properties, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var blobClient = new BlobClient(mockCloudBlobClient.Object);
			blobClient.SetServicePropertiesAsync(properties).Wait();

			// Assert
			mockCloudBlobClient.Verify();
		}
	}
}
