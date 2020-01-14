using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using Picton.Managers;
using System;
using System.Threading;
using Xunit;

namespace Picton.UnitTests.Managers
{
	public class BlobManagerTests
	{
		public class Constructor
		{
			[Fact]
			public void Creates_container_if_does_not_exist()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var connectionString = "UseDevelopmentStorage=true";
				var containerName = "mycontainer";
				var accessType = PublicAccessType.BlobContainer;

				var blobContainerInfo = BlobsModelFactory.BlobContainerInfo(ETag.All, DateTimeOffset.UtcNow);

				var mockBlobContainer = new Mock<BlobContainerClient>(MockBehavior.Strict, connectionString, containerName);
				mockBlobContainer
					.Setup(c => c.CreateIfNotExists(accessType, null, default))
					.Returns(Response.FromValue(blobContainerInfo, new MockAzureResponse(200, "ok")))
					.Verifiable();

				// Act
				new BlobManager(mockBlobContainer.Object, accessType);

				// Assert
				mockBlobContainer.Verify();
			}
		}

		//[Fact]
		//public void GetBlobContentAsync_blob_does_not_exist()
		//{
		//	// Arrange
		//	var containerName = "mycontainer";
		//	var mockBlobContainer = Misc.GetMockBlobContainer(containerName);
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var blobName = "myblob.txt";
		//	var mockBlobUri = new Uri(Misc.BLOB_STORAGE_URL + blobName);

		//	var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, mockBlobUri);
		//	mockBlob
		//		.Setup(b => b.ExistsAsync(It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.ReturnsAsync(false)
		//		.Verifiable();

		//	mockBlobContainer
		//		.Setup(c => c.GetBlobReference(blobName))
		//		.Returns(mockBlob.Object)
		//		.Verifiable();

		//	// Act
		//	var blobManager = new BlobManager(containerName, mockBlobClient.Object);
		//	var result = blobManager.GetBlobContentAsync(blobName, CancellationToken.None);
		//	result.Wait();
		//	var content = result.Result;

		//	// Assert
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	mockBlob.Verify();
		//	content.ShouldBeNull();
		//}

		//[Fact]
		//public void GetBlobContentAsync_blob_exists()
		//{
		//	// Arrange
		//	var containerName = "mycontainer";
		//	var mockBlobContainer = Misc.GetMockBlobContainer(containerName);
		//	var mockBlobClient = Misc.GetMockBlobClient(mockBlobContainer);
		//	var blobName = "myblob.txt";
		//	var mockBlobUri = new Uri(Misc.BLOB_STORAGE_URL + blobName);
		//	var expected = "Hello World!";

		//	var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, mockBlobUri);
		//	mockBlob
		//		.Setup(b => b.ExistsAsync(It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.ReturnsAsync(true)
		//		.Verifiable();
		//	mockBlob
		//		.Setup(b => b.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
		//		.Returns(Task.FromResult(true))
		//		.Callback(async (Stream s, AccessCondition ac, BlobRequestOptions o, OperationContext oc, CancellationToken ct) =>
		//		{
		//			var buffer = expected.ToBytes();
		//			await s.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
		//		})
		//		.Verifiable();

		//	mockBlobContainer
		//		.Setup(c => c.GetBlobReference(blobName))
		//		.Returns(mockBlob.Object)
		//		.Verifiable();

		//	// Act
		//	var blobManager = new BlobManager(containerName, mockBlobClient.Object);
		//	var result = Encoding.UTF8.GetString(blobManager.GetBlobContentAsync(blobName).Result);

		//	// Assert
		//	mockBlobContainer.Verify();
		//	mockBlobClient.Verify();
		//	mockBlob.Verify();
		//	result.ShouldBe(expected);
		//}
	}
}
