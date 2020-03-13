using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
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
		/// Attempt to acquire a lease asynchronously.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <param name="leaseTime">The lease duration. If specified, this value must be between 15 and 60 seconds.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The lease Id.</returns>
		public static async Task<string> TryAcquireLeaseAsync(this BlobContainerClient container, TimeSpan? leaseTime = null, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			if (container == null) throw new ArgumentNullException(nameof(container));
			if (maxLeaseAttempts < 1 || maxLeaseAttempts > 10)
			{
				throw new ArgumentOutOfRangeException(nameof(maxLeaseAttempts), "The number of attempts must be between 1 and 10");
			}

			var leaseId = (string)null;
			for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
			{
				try
				{
					leaseId = await container.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);
					if (!string.IsNullOrEmpty(leaseId)) break;
				}
				catch (RequestFailedException e) when (e.ErrorCode == "LeaseAlreadyPresent")
				{
					if (attempts < maxLeaseAttempts - 1)
					{
						await Task.Delay(500).ConfigureAwait(false);    // Make sure we don't retry too quickly
					}
				}
			}

			return leaseId;
		}

		/// <summary>
		/// Acquire a lease asynchronously.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <param name="leaseTime">The lease duration. If specified, this value must be between 15 and 60 seconds.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The lease Id.</returns>
		public static async Task<string> AcquireLeaseAsync(this BlobContainerClient container, TimeSpan? leaseTime = null, CancellationToken cancellationToken = default)
		{
			// From: https://docs.microsoft.com/en-us/rest/api/storageservices/lease-container
			// The Lease Container operation establishes and manages a lock on a container for delete operations.
			// The lock duration can be 15 to 60 seconds, or can be infinite.
			if (leaseTime.HasValue && (leaseTime.Value < TimeSpan.FromSeconds(15) | leaseTime.Value > TimeSpan.FromSeconds(60)))
			{
				throw new ArgumentOutOfRangeException(nameof(leaseTime), "Lease duration must be between 15 and 60 seconds");
			}

			var leaseClient = new BlobLeaseClient(container, null);
			var defaultLeaseTime = TimeSpan.FromSeconds(15);

			try
			{
				// Optimistically try to acquire the lease. The container may not yet
				// exist. If it doesn't we handle the 404, create it, and retry below
				var response = await leaseClient.AcquireAsync(leaseTime.GetValueOrDefault(defaultLeaseTime), null, cancellationToken).ConfigureAwait(false);
				return response.Value.LeaseId;
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await container.CreateAsync(PublicAccessType.None, null, cancellationToken).ConfigureAwait(false);
				var response = await leaseClient.AcquireAsync(leaseTime.GetValueOrDefault(defaultLeaseTime), null, cancellationToken).ConfigureAwait(false);

				return response.Value.LeaseId;
			}
		}

		/// <summary>
		/// Release a lease.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <param name="leaseId">The lease Id.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task ReleaseLeaseAsync(this BlobContainerClient container, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (container == null) throw new ArgumentNullException(nameof(container));

			var leaseClient = new BlobLeaseClient(container, leaseId);
			return leaseClient.ReleaseAsync(null, cancellationToken);
		}

		/// <summary>
		/// Renews the lease asynchronously.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">blob.</exception>
		public static Task RenewLeaseAsync(this BlobContainerClient container, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (container == null) throw new ArgumentNullException(nameof(container));

			var leaseClient = new BlobLeaseClient(container, leaseId);
			return leaseClient.RenewAsync(null, cancellationToken);
		}

		/// <summary>
		/// Attempts to renew a lease asynchronously.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="bool">boolean</see> value indicating if the lease was obtained.</returns>
		public static async Task<bool> TryRenewLeaseAsync(this BlobContainerClient container, string leaseId = null, CancellationToken cancellationToken = default)
		{
			try
			{
				await RenewLeaseAsync(container, leaseId, cancellationToken).ConfigureAwait(false);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Asynchronously copy a blob.
		/// </summary>
		/// <param name="container">The container.</param>
		/// <param name="sourceBlobName">The name of the blob containing the content to be copied.</param>
		/// <param name="destinationBlobName">The name of the blob where the content will be copied.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="waitForCompletion">Indicates if the call should return immediately or wait until the operation has completed.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task CopyAsync(this BlobContainerClient container, string sourceBlobName, string destinationBlobName, string leaseId = null, bool waitForCompletion = true, CancellationToken cancellationToken = default)
		{
			if (container == null) throw new ArgumentNullException(nameof(container));
			if (string.IsNullOrEmpty(destinationBlobName)) throw new ArgumentNullException(nameof(destinationBlobName));

			var sourceBlob = container.GetBlobClient(sourceBlobName);
			var destinationBlob = container.GetBlobClient(destinationBlobName);

			BlobRequestConditions requestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				requestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			var copyOperation = await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri, null, null, requestConditions, null, null, cancellationToken).ConfigureAwait(false);

			if (waitForCompletion)
			{
				await copyOperation.WaitForCompletionAsync(cancellationToken).ConfigureAwait(false);
			}
		}

		#endregion
	}
}
