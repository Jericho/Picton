using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Shouldly;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.UnitTests.Extensions
{
	public class CloudBlobClientExtensionsTests
	{
		[Fact]
		public async Task ListContainersAsync_success()
		{
			// Arrange
			var prefix = (string)null;
			var listingDetails = ContainerListingDetails.All;
			var maxResults = 100;
			var cancellationToken = CancellationToken.None;

			var expectedContainers = new[]
			{
				new CloudBlobContainer(new Uri(Misc.BLOB_STORAGE_URL + "Container1")),
				new CloudBlobContainer(new Uri(Misc.BLOB_STORAGE_URL + "Container2"))
			};
			var expected = new ContainerResultSegment(expectedContainers, null);

			var mockBlobStorageUri = new Uri(Misc.BLOB_STORAGE_URL);
			var mockBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, mockBlobStorageUri);
			mockBlobClient
				.Setup(c => c.ListContainersSegmentedAsync(prefix, listingDetails, maxResults, null, null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobClient.Object.ListContainersAsync(prefix, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobClient.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(2);
		}

		[Fact]
		public async Task ListContainersAsync_returns_empty_array_when_no_matching_containers()
		{
			// Arrange
			var prefix = (string)null;
			var listingDetails = ContainerListingDetails.All;
			var maxResults = 100;
			var cancellationToken = CancellationToken.None;

			var expectedContainers = new CloudBlobContainer[] { };
			var expected = new ContainerResultSegment(expectedContainers, null);

			var mockBlobStorageUri = new Uri(Misc.BLOB_STORAGE_URL);
			var mockBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, mockBlobStorageUri);
			mockBlobClient
				.Setup(c => c.ListContainersSegmentedAsync(prefix, listingDetails, maxResults, null, null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobClient.Object.ListContainersAsync(prefix, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobClient.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(0);
		}

		[Fact]
		public async Task ListContainersAsync_does_not_return_more_results_than_desired()
		{
			// Arrange
			var prefix = (string)null;
			var listingDetails = ContainerListingDetails.All;
			var maxResults = 3;
			var cancellationToken = CancellationToken.None;

			var expectedContainers = new[]
			{
				new CloudBlobContainer(new Uri(Misc.BLOB_STORAGE_URL + "Container1")),
				new CloudBlobContainer(new Uri(Misc.BLOB_STORAGE_URL + "Container2"))
			};
			var expected = new ContainerResultSegment(expectedContainers, new BlobContinuationToken());

			var mockBlobStorageUri = new Uri(Misc.BLOB_STORAGE_URL);
			var mockBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, mockBlobStorageUri);
			mockBlobClient
				.Setup(c => c.ListContainersSegmentedAsync(prefix, listingDetails, maxResults, It.IsAny<BlobContinuationToken>(), null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobClient.Object.ListContainersAsync(prefix, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobClient.Verify();
			result.ShouldNotBeNull();
			result.Count().ShouldBe(maxResults);
		}
	}
}
