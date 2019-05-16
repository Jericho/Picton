using Microsoft.Azure.Storage.Blob;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.UnitTests.Extensions
{
	public class CloudBlobContainerExtensionsTests
	{
		[Fact]
		public async Task ListBlobsAsync_success()
		{
			// Arrange
			var prefix = (string)null;
			var includeSubFolders = false;
			var listingDetails = BlobListingDetails.All;
			var maxResults = 100;
			var cancellationToken = CancellationToken.None;

			var expectedBlobs = new[]
			{
				new CloudBlockBlob(new Uri(Misc.BLOB_STORAGE_URL + "Blob1.txt")),
				new CloudBlockBlob(new Uri(Misc.BLOB_STORAGE_URL + "Blob2.txt"))
			};
			var expected = new BlobResultSegment(expectedBlobs, null);

			var mockContainerUri = new Uri(Misc.BLOB_STORAGE_URL + "MyContainer");
			var mockBlobContainer = new Mock<CloudBlobContainer>(MockBehavior.Strict, mockContainerUri);
			mockBlobContainer
				.Setup(c => c.ListBlobsSegmentedAsync(prefix, includeSubFolders, listingDetails, maxResults, null, null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobContainer.Object.ListBlobsAsync(prefix, includeSubFolders, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobContainer.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(2);
		}

		[Fact]
		public async Task ListBlobsAsync_returns_empty_array_when_no_matching_blobs()
		{
			// Arrange
			var prefix = (string)null;
			var includeSubFolders = false;
			var listingDetails = BlobListingDetails.All;
			var maxResults = 100;
			var cancellationToken = CancellationToken.None;

			var expectedBlobs = new CloudBlockBlob[] { };
			var expected = new BlobResultSegment(expectedBlobs, null);

			var mockContainerUri = new Uri(Misc.BLOB_STORAGE_URL + "MyContainer");
			var mockBlobContainer = new Mock<CloudBlobContainer>(MockBehavior.Strict, mockContainerUri);
			mockBlobContainer
				.Setup(c => c.ListBlobsSegmentedAsync(prefix, includeSubFolders, listingDetails, maxResults, It.IsAny<BlobContinuationToken>(), null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobContainer.Object.ListBlobsAsync(prefix, includeSubFolders, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobContainer.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(0);
		}

		[Fact]
		public async Task ListBlobsAsync_does_not_return_more_results_than_desired()
		{
			// Arrange
			var prefix = (string)null;
			var includeSubFolders = false;
			var listingDetails = BlobListingDetails.All;
			var maxResults = 3;
			var cancellationToken = CancellationToken.None;

			var expectedBlobs = new[]
			{
				new CloudBlockBlob(new Uri(Misc.BLOB_STORAGE_URL + "Blob1.txt")),
				new CloudBlockBlob(new Uri(Misc.BLOB_STORAGE_URL + "Blob2.txt"))
			};
			var expected = new BlobResultSegment(expectedBlobs, new BlobContinuationToken());

			var mockContainerUri = new Uri(Misc.BLOB_STORAGE_URL + "MyContainer");
			var mockBlobContainer = new Mock<CloudBlobContainer>(MockBehavior.Strict, mockContainerUri);
			mockBlobContainer
				.Setup(c => c.ListBlobsSegmentedAsync(prefix, includeSubFolders, listingDetails, maxResults, It.IsAny<BlobContinuationToken>(), null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobContainer.Object.ListBlobsAsync(prefix, includeSubFolders, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobContainer.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(maxResults);
		}
	}
}
