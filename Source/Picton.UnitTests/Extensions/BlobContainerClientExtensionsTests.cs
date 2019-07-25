using Azure;
using Azure.Core.Pipeline;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Moq;
using Shouldly;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.UnitTests.Extensions
{
	public class BlobContainerClientExtensionsTests
	{
		[Fact]
		public async Task Exists_true()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var connectionString = "UseDevelopmentStorage=true";
			var containerName = "mycontainer";
			var containerItem = new ContainerItem();
			var expected = new Response<ContainerItem>();

			var mockBlobContainer = new Mock<BlobContainerClient>(MockBehavior.Strict, connectionString, containerName);
			mockBlobContainer
				.Setup(c => c.GetPropertiesAsync(null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlobContainer.Object.ExistsAsync(cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobContainer.Verify();
			result.ShouldBeTrue();
		}

		[Fact]
		public async Task Exists_false()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var connectionString = "UseDevelopmentStorage=true";
			var containerName = "mycontainer";

			var response = new MockAzureResponse(400, "The specified container does not exist.");
			response.AddHeader(new HttpHeader("x-ms-error-code", "ContainerNotFound"));

			var mockBlobContainer = new Mock<BlobContainerClient>(MockBehavior.Strict, connectionString, containerName);
			mockBlobContainer
				.Setup(c => c.GetPropertiesAsync(null, cancellationToken))
				.ThrowsAsync(new StorageRequestFailedException(response))
				.Verifiable();

			// Act
			var result = await mockBlobContainer.Object.ExistsAsync(cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobContainer.Verify();
			result.ShouldBeFalse();
		}
	}
}
