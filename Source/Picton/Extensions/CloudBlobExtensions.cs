using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Picton.Interfaces;
using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	public static class CloudBlobExtensions
	{
		#region PUBLIC EXTENSION METHODS

		public static async Task<string> TryAcquireLeaseAsync(this CloudBlob blob, TimeSpan? leaseTime = null, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (maxLeaseAttempts < 1 || maxLeaseAttempts > 10)
			{
				throw new ArgumentOutOfRangeException(string.Format("{0} must be between 1 and 10", nameof(maxLeaseAttempts)));
			}

			var leaseId = (string)null;
			for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
			{
				try
				{
					leaseId = await blob.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);
					if (!string.IsNullOrEmpty(leaseId)) break;
				}
				catch (StorageException e)
				{
					// If the status code is 409 (HttpStatusCode.Conflict), it means the resource is already leased
					if (e.RequestInformation?.HttpStatusCode == 409)
					{
						if (attempts < maxLeaseAttempts - 1)
						{
							await Task.Delay(500).ConfigureAwait(false);    // Make sure we don't retry too quickly
						}
					}
					else
					{
						throw;
					}
				}
			}

			return leaseId;
		}

		public static async Task<string> AcquireLeaseAsync(this CloudBlob blob, TimeSpan? leaseTime = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			// From: https://msdn.microsoft.com/en-us/library/azure/ee691972.aspx
			// The Lease Blob operation establishes and manages a lock on a blob for write and delete operations.
			// The lock duration can be 15 to 60 seconds, or can be infinite.
			if (leaseTime.HasValue && (leaseTime.Value < TimeSpan.FromSeconds(15) | leaseTime.Value > TimeSpan.FromSeconds(60)))
			{
				throw new ArgumentOutOfRangeException(nameof(leaseTime), string.Format("{0} must be between 15 and 60 seconds", nameof(leaseTime)));
			}

			var defaultLeaseTime = TimeSpan.FromSeconds(15);
			var proposedLeaseId = (string)null; // Proposed lease id (leave it null for storage service to return you one).
			var blobDoesNotExist = false;
			var leaseId = (string)null;

			leaseTime = leaseTime.HasValue ? leaseTime : defaultLeaseTime;

			try
			{
				// Optimistically try to acquire the lease. The blob may not yet
				// exist. If it doesn't we handle the 404, create it, and retry below
				leaseId = await blob.AcquireLeaseAsync(leaseTime, proposedLeaseId, null, null, null, cancellationToken).ConfigureAwait(false);
			}
			catch (StorageException e)
			{
				if (e.RequestInformation?.HttpStatusCode == 404)
				{
					blobDoesNotExist = true;
				}
				else
				{
					throw;
				}
			}

			if (blobDoesNotExist)
			{
				await blob.UploadTextAsync(string.Empty, null, cancellationToken);
				leaseId = await blob.AcquireLeaseAsync(leaseTime, proposedLeaseId, null, null, null, cancellationToken).ConfigureAwait(false);
			}

			return leaseId;
		}

		public static Task ReleaseLeaseAsync(this CloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			var accessCondition = new AccessCondition { LeaseId = leaseId };
			return blob.ReleaseLeaseAsync(accessCondition, null, null, cancellationToken);
		}

		/// <summary>
		/// Renews the lease asynchronously.
		/// </summary>
		/// <param name="blob">The BLOB.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns></returns>
		/// <exception cref="System.ArgumentNullException">blob</exception>
		/// <remarks>
		/// Starting in version 2012-02-12, some behaviors of the Lease Blob operation differ from
		/// previous versions. For example, in previous versions of the Lease Blob operation you
		/// could renew a lease after releasing it. Starting in version 2012-02-12, this lease
		/// request will fail, while calls using older versions of Lease Blob still succeed. See
		/// the Changes to Lease Blob introduced in version 2012-02-12 section under Remarks for a
		/// list of changes to the behavior of this operation.
		/// </remarks>
		public static Task RenewLeaseAsync(this CloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			var accessCondition = new AccessCondition { LeaseId = leaseId };
			return blob.RenewLeaseAsync(accessCondition, null, null, cancellationToken);
		}

		public static async Task<bool> TryRenewLeaseAsync(this CloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				await RenewLeaseAsync(blob, leaseId, cancellationToken);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static async Task UploadStreamAsync(this CloudBlob blob, Stream stream, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			stream.Position = 0; // Rewind the stream. IMPORTANT!

			var accessCondition = string.IsNullOrEmpty(leaseId) ? null : new AccessCondition { LeaseId = leaseId };

			if (blob is CloudAppendBlob)
			{
				var appendBlob = blob as CloudAppendBlob;
				await appendBlob.CreateOrReplaceAsync(accessCondition, null, null, cancellationToken).ConfigureAwait(false);
				await appendBlob.AppendFromStreamAsync(stream, accessCondition, null, null, cancellationToken);
			}
			else if (blob is CloudBlockBlob)
			{
				var blockBlob = blob as CloudBlockBlob;
				await blockBlob.UploadFromStreamAsync(stream, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
			}
			else if (blob is CloudPageBlob)
			{
				var pageBlob = blob as CloudPageBlob;
				await pageBlob.UploadFromStreamAsync(stream, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				throw new Exception($"Unknow blob type: {blob.GetType().Name}");
			}
		}

		public static Task UploadBytesAsync(this CloudBlob blob, byte[] buffer, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var stream = new MemoryStream(buffer);
			return blob.UploadStreamAsync(stream, leaseId, cancellationToken);
		}

		public static Task UploadTextAsync(this CloudBlob blob, string content, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var buffer = content.ToBytes();
			return blob.UploadBytesAsync(buffer, leaseId, cancellationToken);
		}

		public static async Task AppendStreamAsync(this CloudBlob blob, Stream stream, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			stream.Position = 0; // Rewind the stream. IMPORTANT!

			var blobExits = await blob.ExistsAsync(null, null, cancellationToken).ConfigureAwait(false);
			var accessCondition = string.IsNullOrEmpty(leaseId) ? null : new AccessCondition { LeaseId = leaseId };

			if (blob is CloudAppendBlob)
			{
				var appendBlob = blob as CloudAppendBlob;
				if (!blobExits)
				{
					await appendBlob.CreateOrReplaceAsync(accessCondition, null, null, cancellationToken).ConfigureAwait(false);
				}

				await appendBlob.AppendFromStreamAsync(stream, accessCondition, null, null, cancellationToken);
			}
			else if (blob is CloudBlockBlob)
			{
				var blockBlob = blob as CloudBlockBlob;
				if (blobExits)
				{
					var content = new MemoryStream();
					await blockBlob.DownloadToStreamAsync(content, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
					await stream.CopyToAsync(content).ConfigureAwait(false);
					content.Position = 0; // Rewind the stream. IMPORTANT!
					await blockBlob.UploadFromStreamAsync(content, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					await blockBlob.UploadFromStreamAsync(stream, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
				}
			}
			else if (blob is CloudPageBlob)
			{
				var pageBlob = blob as CloudPageBlob;
				if (blobExits)
				{
					var content = new MemoryStream();
					await pageBlob.DownloadToStreamAsync(content, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
					await stream.CopyToAsync(content).ConfigureAwait(false);
					content.Position = 0; // Rewind the stream. IMPORTANT!
					await pageBlob.UploadFromStreamAsync(content, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
				}
				else
				{
					await pageBlob.UploadFromStreamAsync(stream, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
				}
			}
			else
			{
				throw new Exception($"Unknow blob type: {blob.GetType().Name}");
			}
		}

		public static Task AppendBytesAsync(this CloudBlob blob, byte[] buffer, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var stream = new MemoryStream(buffer);
			return blob.AppendStreamAsync(stream, leaseId, cancellationToken);
		}

		public static Task AppendTextAsync(this CloudBlob blob, string content, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var buffer = content.ToBytes();
			return blob.AppendBytesAsync(buffer, leaseId, cancellationToken);
		}

		public static Task SetMetadataAsync(this CloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var accessCondition = new AccessCondition();
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessCondition.LeaseId = leaseId;
			}

			return blob.SetMetadataAsync(accessCondition, null, null, cancellationToken);
		}

		public static async Task<string> DownloadTextAsync(this CloudBlob blob, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			using (var stream = new MemoryStream())
			{
				await blob.DownloadToStreamAsync(stream, null, null, null, cancellationToken).ConfigureAwait(false);
				using (var reader = new StreamReader(stream, true))
				{
					stream.Position = 0;
					return await reader.ReadToEndAsync();
				}
			}
		}

		public static async Task<byte[]> DownloadByteArrayAsync(this CloudBlob blob, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			using (var ms = new MemoryStream())
			{
				await blob.DownloadToStreamAsync(ms, null, null, null, cancellationToken).ConfigureAwait(false);
				ms.Position = 0;
				return ms.ToArray();
			}
		}

		public static async Task CopyAsync(this CloudBlob blob, string destinationBlobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			var container = blob.Container;
			async Task WaitForCopyCompletion(CopyState copyState)
			{
				while (copyState.Status == CopyStatus.Pending)
					await Task.Delay(100).ConfigureAwait(false);
				if (copyState.Status != CopyStatus.Success)
					throw new Exception($"CopyAsync failed: {copyState.Status}");
			}

			if (blob is CloudAppendBlob)
			{
				var appendTarget = container.GetAppendBlobReference(destinationBlobName);
				await appendTarget.StartCopyAsync((CloudAppendBlob)blob, null, null, null, null, cancellationToken).ConfigureAwait(false);
				await WaitForCopyCompletion(appendTarget.CopyState).ConfigureAwait(false);
			}
			else if (blob is CloudBlockBlob)
			{
				var blockTarget = container.GetBlockBlobReference(destinationBlobName);
				await blockTarget.StartCopyAsync((CloudBlockBlob)blob, null, null, null, null, cancellationToken).ConfigureAwait(false);
				await WaitForCopyCompletion(blockTarget.CopyState).ConfigureAwait(false);
			}
			else if (blob is CloudPageBlob)
			{
				var pageTarget = container.GetPageBlobReference(destinationBlobName);
				await pageTarget.StartCopyAsync((CloudPageBlob)blob, null, null, null, null, cancellationToken).ConfigureAwait(false);
				await WaitForCopyCompletion(pageTarget.CopyState).ConfigureAwait(false);
			}
			else
			{
				throw new Exception($"Unknow blob type: {blob.GetType().Name}");
			}
		}

		/// <summary>
		/// Creates a Shared Access Signature URI for the blob.
		/// </summary>
		/// <param name="blob">The blob to be shared</param>
		/// <param name="permissions">The permissions granted to the shared access signature</param>
		/// <param name="duration">The period of time the shared access signature is valid for. If this parameter is omited, it defaults to 15 minutes.</param>
		/// <param name="systemClock">Allows dependency injection for unit tesing puposes. Feel free to ignore this parameter.</param>
		/// <returns>The URI</returns>
		/// <remarks>
		/// Inspired by http://gauravmantri.com/2013/02/13/revisiting-windows-azure-shared-access-signature/
		/// </remarks>
		public static string GetSharedAccessSignatureUri(this CloudBlob blob, SharedAccessBlobPermissions permissions, TimeSpan? duration = null, ISystemClock systemClock = null)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var now = (systemClock ?? SystemClock.Instance).UtcNow;
			var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
			{
				Permissions = permissions,
				SharedAccessStartTime = now.AddMinutes(-5), // Start time is back by 5 minutes to take clock skewness into consideration
				SharedAccessExpiryTime = now.Add(duration.GetValueOrDefault(TimeSpan.FromMinutes(15)))
			});
			return string.Format(CultureInfo.InvariantCulture, "{0}{1}", blob.Uri, sas);
		}

		#endregion
	}
}
