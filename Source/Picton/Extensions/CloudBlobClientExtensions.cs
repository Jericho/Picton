using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="CloudBlobClient"/> class.
	/// </summary>
	public static class CloudBlobClientExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Lists the containers in a storage account.
		/// </summary>
		/// <param name="blobClient">The blob client</param>
		/// <param name="prefix">Prefix</param>
		/// <param name="listingDetails">Specifies which details to include when listing the containers</param>
		/// <param name="maxResults">The maximum number of containers to include in the result</param>
		/// <param name="cancellationToken">The cancellation token</param>
		/// <returns>The list of containers</returns>
		public static async Task<IEnumerable<CloudBlobContainer>> ListContainersAsync(this CloudBlobClient blobClient, string prefix = null, ContainerListingDetails listingDetails = ContainerListingDetails.Metadata, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var continuationToken = (BlobContinuationToken)null;
			var containers = new List<CloudBlobContainer>();
			var maxNumberOfContainers = maxResults.GetValueOrDefault(int.MaxValue);

			do
			{
				var response = await blobClient.ListContainersSegmentedAsync(prefix, listingDetails, maxResults, continuationToken, null, null, cancellationToken);
				continuationToken = response.ContinuationToken;
				containers.AddRange(response.Results);
			}
			while (continuationToken != null && containers.Count < maxNumberOfContainers);

			return containers
				.Take(maxNumberOfContainers)
				.ToArray();
		}

		#endregion
	}
}
