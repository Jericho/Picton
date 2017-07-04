using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	public static class CloudBlobDirectoryExtensions
	{
		#region PUBLIC EXTENSION METHODS

		public static async Task<IEnumerable<IListBlobItem>> ListBlobsAsync(this CloudBlobDirectory blobFolder, bool includeSubFolders = false, BlobListingDetails listingDetails = BlobListingDetails.Metadata, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var continuationToken = (BlobContinuationToken)null;
			var blobs = new List<IListBlobItem>();

			do
			{
				var response = await blobFolder.ListBlobsSegmentedAsync(includeSubFolders, listingDetails, maxResults, continuationToken, null, null, cancellationToken);
				continuationToken = response.ContinuationToken;
				blobs.AddRange(response.Results);
			}
			while (continuationToken != null || (maxResults.HasValue && blobs.Count >= maxResults.Value));

			return blobs;
		}

		#endregion
	}
}
