using Azure.Storage;
using Azure.Storage.Blobs;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="BlobContainerClient"/> class.
	/// </summary>
	public static class BlobContainerClientExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Indicates if a container exists.
		/// </summary>
		/// <param name="blobContainer">The container.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the container exists; false otherwise.</returns>
		public static async Task<bool> ExistsAsync(this BlobContainerClient blobContainer, CancellationToken cancellationToken = default)
		{
			if (blobContainer == null) throw new ArgumentNullException(nameof(blobContainer));

			try
			{
				var properties = await blobContainer.GetPropertiesAsync(null, cancellationToken).ConfigureAwait(false);
				return true;
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "ContainerNotFound")
			{
				return false;
			}
		}

		/// <summary>
		/// Creates a container if it doesn't already exists.
		/// </summary>
		/// <param name="blobContainer">The container.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the container was created; false otherwise.</returns>
		public static async Task<bool> CreateIfNotExistsAsync(this BlobContainerClient blobContainer, CancellationToken cancellationToken = default)
		{
			if (blobContainer == null) throw new ArgumentNullException(nameof(blobContainer));

			var exists = await blobContainer.ExistsAsync(cancellationToken).ConfigureAwait(false);

			if (!exists)
			{
				await blobContainer.CreateAsync(null, null, cancellationToken).ConfigureAwait(false);
				return true;
			}

			return false;
		}

		#endregion
	}
}
