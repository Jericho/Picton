using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	public static class CloudBlobClientExtensions
	{
		#region PUBLIC EXTENSION METHODS

		public static async Task<IEnumerable<CloudBlobContainer>> ListContainersAsync(this CloudBlobClient blobClient, string prefix = null, ContainerListingDetails listingDetails = ContainerListingDetails.Metadata, int ? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var continuationToken = (BlobContinuationToken)null;
			var containers = new List<CloudBlobContainer>();

			do
			{
				var response = await blobClient.ListContainersSegmentedAsync(prefix, listingDetails, maxResults, continuationToken, null, null, cancellationToken);
				continuationToken = response.ContinuationToken;
				containers.AddRange(response.Results);
			}
			while (continuationToken != null || (maxResults.HasValue && containers.Count >= maxResults.Value));

			return containers;
		}

		#endregion
	}
}
