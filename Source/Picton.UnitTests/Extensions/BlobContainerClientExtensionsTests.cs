using Azure;
using Azure.Core;
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

			var mockResponse = new Mock<Response<BlobContainerProperties>>();

			var mockBlobContainer = new Mock<BlobContainerClient>(MockBehavior.Strict, connectionString, containerName);
			mockBlobContainer
				.Setup(c => c.GetPropertiesAsync(null, cancellationToken))
				.ReturnsAsync(mockResponse.Object)
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
			var errorCode = "ContainerNotFound";

			var response = new MockAzureResponse(400, "The specified container does not exist.");
			response.AddHeader(new HttpHeader("x-ms-error-code", errorCode));

			var mockBlobContainer = new Mock<BlobContainerClient>(MockBehavior.Strict, connectionString, containerName);
			mockBlobContainer
				.Setup(c => c.GetPropertiesAsync(null, cancellationToken))
				.ThrowsAsync(new RequestFailedException(response.Status, response.ReasonPhrase, errorCode, null))
				.Verifiable();

			// Act
			var result = await mockBlobContainer.Object.ExistsAsync(cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlobContainer.Verify();
			result.ShouldBeFalse();
		}
	}
}
