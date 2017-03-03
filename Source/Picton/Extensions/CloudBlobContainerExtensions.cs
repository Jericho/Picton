using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	public static class CloudBlobContainerExtensions
	{
		#region PUBLIC EXTENSION METHODS

		public static async Task<IEnumerable<IListBlobItem>> ListBlobsAsync(this CloudBlobContainer blobContainer, string prefix, bool includeSubFolders = false, BlobListingDetails listingDetails = BlobListingDetails.Metadata, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var continuationToken = (BlobContinuationToken)null;
			var blobs = new List<IListBlobItem>();

			do
			{
				var response = await blobContainer.ListBlobsSegmentedAsync(prefix, includeSubFolders, listingDetails, maxResults, continuationToken, null, null, cancellationToken);
				continuationToken = response.ContinuationToken;
				blobs.AddRange(response.Results);
			}
			while (continuationToken != null || (maxResults.HasValue && blobs.Count >= maxResults.Value));

			return blobs;
		}

		public static async Task<IEnumerable<CloudBlobDirectory>> ListSubFoldersAsync(this CloudBlobContainer blobContainer, string parentFolder, BlobListingDetails listingDetails = BlobListingDetails.None, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (string.IsNullOrEmpty(parentFolder))
			{
				var blobs = await blobContainer.ListBlobsAsync(parentFolder, false, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);
				var subFolders = blobs.OfType<CloudBlobDirectory>();
				return subFolders;
			}
			else
			{
				var blobs = await blobContainer.GetDirectoryReference(parentFolder).ListBlobsAsync(false, listingDetails, maxResults, cancellationToken).ConfigureAwait(false);
				var subFolders = blobs.OfType<CloudBlobDirectory>();
				return subFolders;
			}
		}

		#endregion
	}
}
