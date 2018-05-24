using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="CloudBlobContainer"/> class.
	/// </summary>
	public static class CloudBlobContainerExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Lists the blobs in a storage container
		/// </summary>
		/// <param name="blobContainer">The storage container</param>
		/// <param name="prefix">Prefix</param>
		/// <param name="includeSubFolders">Indicates whether to list blobs in a flat listing or to list blobs hierarchically, by virtual directory.</param>
		/// <param name="listingDetails">Specifies which details to include when listing the blobs</param>
		/// <param name="maxResults">The maximum number of blobs to include in the result</param>
		/// <param name="cancellationToken">The cancellation token</param>
		/// <returns>The list of blobs</returns>
		public static async Task<IEnumerable<IListBlobItem>> ListBlobsAsync(this CloudBlobContainer blobContainer, string prefix, bool includeSubFolders = false, BlobListingDetails listingDetails = BlobListingDetails.Metadata, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var continuationToken = (BlobContinuationToken)null;
			var blobs = new List<IListBlobItem>();
			var maxNumberOfItems = maxResults.GetValueOrDefault(int.MaxValue);

			do
			{
				var response = await blobContainer.ListBlobsSegmentedAsync(prefix, includeSubFolders, listingDetails, maxResults, continuationToken, null, null, cancellationToken);
				continuationToken = response.ContinuationToken;
				blobs.AddRange(response.Results);
			}
			while (continuationToken != null && blobs.Count < maxNumberOfItems);

			return blobs
				.Take(maxNumberOfItems)
				.ToArray();
		}

		/// <summary>
		/// Lists the sub-directories that are present in a folder.
		/// </summary>
		/// <param name="blobContainer">The storage container</param>
		/// <param name="parentFolder">The parent folder</param>
		/// <param name="listingDetails">Specifies which details to include when listing the sub-folders</param>
		/// <param name="maxResults">The maximum number of sub-folders to include in the result</param>
		/// <param name="cancellationToken">The cancellation token</param>
		/// <returns>The list of blobs</returns>
		public static async Task<IEnumerable<CloudBlobDirectory>> ListSubFoldersAsync(this CloudBlobContainer blobContainer, string parentFolder = null, BlobListingDetails listingDetails = BlobListingDetails.None, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
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
