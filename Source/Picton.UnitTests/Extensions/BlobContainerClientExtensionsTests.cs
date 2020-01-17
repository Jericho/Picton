using Azure.Storage.Blobs;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.UnitTests.Extensions
{
	public class BlobContainerClientExtensionsTests
	{
		public class CopyAsync
		{
			[Fact]
			public async Task Throws_when_container_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var sourceBlobName = "SourceBlob.txt";
				var destinationBlobName = "DestinationBlob.txt";

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobContainerClient)null).CopyAsync(sourceBlobName, destinationBlobName, null, true, cancellationToken)).ConfigureAwait(false);
			}
		}
	}
}
