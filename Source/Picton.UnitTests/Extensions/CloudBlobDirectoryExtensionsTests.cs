namespace Picton.UnitTests.Extensions
{
	public class CloudBlobDirectoryExtensionsTests
	{
		//CloudBlobDirectory does not define any public constructor which prevents Moq from instanciating an instance of this type
		//Therefore, we can't mock like so: var mockBlobDirectory = new Mock<CloudBlobDirectory>(MockBehavior.Strict);
		//Commenting the unit tests until https://github.com/Azure/azure-storage-net/issues/639 is resolved

		/*
		[Fact]
		public async Task ListBlobsAsync_success()
		{
			// Arrange
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

			var mockBlobDirectory = new Mock<CloudBlobDirectory>(MockBehavior.Strict);
			mockBlobDirectory
				.Setup(c => c.ListBlobsSegmentedAsync(includeSubFolders, listingDetails, maxResults, null, null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobDirectory.Object.ListBlobsAsync(includeSubFolders, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobDirectory.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(2);
		}

		[Fact]
		public async Task ListBlobsAsync_returns_empty_array_when_no_matching_blobs()
		{
			// Arrange
			var includeSubFolders = false;
			var listingDetails = BlobListingDetails.All;
			var maxResults = 100;
			var cancellationToken = CancellationToken.None;

			var expectedBlobs = new CloudBlockBlob[] { };
			var expected = new BlobResultSegment(expectedBlobs, null);

			var mockBlobDirectory = new Mock<CloudBlobDirectory>(MockBehavior.Strict);
			mockBlobDirectory
				.Setup(c => c.ListBlobsSegmentedAsync(includeSubFolders, listingDetails, maxResults, null, null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobDirectory.Object.ListBlobsAsync(includeSubFolders, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobDirectory.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(0);
		}

		[Fact]
		public async Task ListBlobsAsync_does_not_return_more_results_than_desired()
		{
			// Arrange
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

			var mockBlobDirectory = new Mock<CloudBlobDirectory>(MockBehavior.Strict);
			mockBlobDirectory
				.Setup(c => c.ListBlobsSegmentedAsync(includeSubFolders, listingDetails, maxResults, It.IsAny<BlobContinuationToken>(), null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobDirectory.Object.ListBlobsAsync(includeSubFolders, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobDirectory.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(maxResults);
		}
		*/
	}
}
