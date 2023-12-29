using Azure;
using Azure.Storage.Blobs.Models;
using NSubstitute;
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
				var queueManager = new BlobManager(mockBlobContainer, PublicAccessType.None);

				// Assert
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
				.When(callInfo => callInfo.DownloadAsync(cancellationToken))
				.Do(callInfo => throw new RequestFailedException(404, "Blob does not exist", "BlobNotFound", new Exception("???")));

			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, new[] { mockBlobClient });

			// Act
			var blobManager = new BlobManager(mockBlobContainer, PublicAccessType.None);
			var downloadInfo = await blobManager.GetBlobContentAsync(blobName, CancellationToken.None);

			// Assert
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
				.DownloadAsync(cancellationToken)
				.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));

			var mockBlobContainer = MockUtils.GetMockBlobContainerClient(containerName, new[] { mockBlobClient });

			// Act
			var blobManager = new BlobManager(mockBlobContainer, PublicAccessType.BlobContainer);
			var downloadInfo = await blobManager.GetBlobContentAsync(blobName, CancellationToken.None);
			var reader = new StreamReader(downloadInfo.Content);
			var content = reader.ReadToEnd();

			// Assert
			content.ShouldBe(expected);
		}
	}
}
