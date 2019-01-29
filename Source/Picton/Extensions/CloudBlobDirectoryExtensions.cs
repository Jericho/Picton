using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="CloudBlobDirectory"/> class.
	/// </summary>
	public static class CloudBlobDirectoryExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Lists the blobs in a blob directory (AKA virtual directory).
		/// </summary>
		/// <param name="blobFolder">The directory</param>
		/// <param name="includeSubFolders">Indicates whether to list blobs in a flat listing or to list blobs hierarchically, by virtual directory.</param>
		/// <param name="listingDetails">Specifies which details to include when listing the blobs.</param>
		/// <param name="maxResults">The maximum number of blobs to include in the result.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The list of blobs</returns>
		public static async Task<IEnumerable<IListBlobItem>> ListBlobsAsync(this CloudBlobDirectory blobFolder, bool includeSubFolders = false, BlobListingDetails listingDetails = BlobListingDetails.Metadata, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var continuationToken = (BlobContinuationToken)null;
			var blobs = new List<IListBlobItem>();
			var maxNumberOfItems = maxResults.GetValueOrDefault(int.MaxValue);

			do
			{
				var response = await blobFolder.ListBlobsSegmentedAsync(includeSubFolders, listingDetails, maxResults, continuationToken, null, null, cancellationToken).ConfigureAwait(false);
				continuationToken = response.ContinuationToken;
				blobs.AddRange(response.Results);
			}
			while (continuationToken != null && blobs.Count < maxNumberOfItems);

			return blobs
				.Take(maxNumberOfItems)
				.ToArray();
		}

		#endregion
	}
}
