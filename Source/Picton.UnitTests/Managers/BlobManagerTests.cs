using Azure;
using Azure.Storage.Blobs.Models;
using Moq;
using Picton.Managers;
using Shouldly;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
				var containerName = "mycontainer";
				var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName);

				// Act
				new BlobManager(mockBlobContainer.Object, PublicAccessType.None);

				// Assert
				mockBlobContainer.Verify();
			}
		}

		[Fact]
		public async Task GetBlobContentAsync_blob_does_not_exist()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var containerName = "mycontainer";
			var blobName = "myBlob.txt";

			var mockBlobClient = MockUtils.GetMockBlobClient(blobName);
			mockBlobClient
				.Setup(b => b.DownloadAsync(cancellationToken))
				.ThrowsAsync(new RequestFailedException(404, "Blob does not exist", "BlobNotFound", new Exception("???")))
				.Verifiable();

			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, new[] { mockBlobClient });

			// Act
			var blobManager = new BlobManager(mockBlobContainer.Object, PublicAccessType.None);
			var downloadInfo = await blobManager.GetBlobContentAsync(blobName, CancellationToken.None).ConfigureAwait(false);

			// Assert
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			downloadInfo.ShouldBeNull();
		}

		[Fact]
		public async Task GetBlobContentAsync_blob_exists()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var containerName = "mycontainer";
			var blobName = "myBlob.txt";
			var expected = "Hello World!";

			var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(expected.ToBytes()));

			var mockBlobClient = MockUtils.GetMockBlobClient(blobName);
			mockBlobClient
				.Setup(b => b.DownloadAsync(cancellationToken))
				.ReturnsAsync(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")))
				.Verifiable();

			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, new[] { mockBlobClient });

			// Act
			var blobManager = new BlobManager(mockBlobContainer.Object, PublicAccessType.BlobContainer);
			var downloadInfo = await blobManager.GetBlobContentAsync(blobName, CancellationToken.None).ConfigureAwait(false);
			var reader = new StreamReader(downloadInfo.Content);
			var content = reader.ReadToEnd();

			// Assert
			mockBlobContainer.Verify();
			mockBlobClient.Verify();
			content.ShouldBe(expected);
		}
	}
}
