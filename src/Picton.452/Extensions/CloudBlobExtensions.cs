using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Picton.Interfaces;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	public static class CloudBlobExtensions
	{
		#region PUBLIC EXTENSION METHODS

		public static async Task<string> TryAcquireLeaseAsync(this ICloudBlob blob, TimeSpan? leaseTime = null, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
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
				catch (WebException e)
				{
					if (e.Response != null && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.Conflict) // 409, already leased
					{
						e.Response.Close();
						if (attempts < maxLeaseAttempts - 1)
						{
							await Task.Delay(500);    // Make sure we don't attempt too quickly
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

		public static async Task<string> AcquireLeaseAsync(this ICloudBlob blob, TimeSpan? leaseTime = null, CancellationToken cancellationToken = default(CancellationToken))
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
			var leaseId = await blob.AcquireLeaseAsync(leaseTime.GetValueOrDefault(defaultLeaseTime), proposedLeaseId, cancellationToken).ConfigureAwait(false);
			return leaseId;
		}

		public static Task ReleaseLeaseAsync(this ICloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			var accessCondition = new AccessCondition { LeaseId = leaseId };
			return blob.ReleaseLeaseAsync(accessCondition, cancellationToken);
		}

		public static Task RenewLeaseAsync(this ICloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			var accessCondition = new AccessCondition { LeaseId = leaseId };
			return blob.RenewLeaseAsync(accessCondition, cancellationToken);
		}

		public static async Task<bool> TryRenewLeaseAsync(this ICloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			try
			{
				await RenewLeaseAsync(blob, leaseId, cancellationToken);
				return true;
			}
			catch { return false; }
		}

		public static async Task UploadStreamAsync(this ICloudBlob blob, Stream stream, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			stream.Position = 0; // Rewind the stream. IMPORTANT!

			var accessCondition = (string.IsNullOrEmpty(leaseId) ? null : new AccessCondition { LeaseId = leaseId });

			if (blob is CloudAppendBlob)
			{
				var appendBlob = blob as CloudAppendBlob;
				await appendBlob.CreateOrReplaceAsync(accessCondition, null, null, cancellationToken).ConfigureAwait(false);
				await appendBlob.AppendFromStreamAsync(stream, accessCondition, null, null, cancellationToken);
			}
			else
			{
				await blob.UploadFromStreamAsync(stream, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
			}
		}

		public static Task UploadBytesAsync(this ICloudBlob blob, byte[] buffer, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var stream = new MemoryStream(buffer);
			return blob.UploadStreamAsync(stream, leaseId, cancellationToken);
		}

		public static Task UploadTextAsync(this ICloudBlob blob, string content, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var buffer = Encoding.UTF8.GetBytes(content);
			return blob.UploadBytesAsync(buffer, leaseId, cancellationToken);
		}

		public static async Task AppendStreamAsync(this ICloudBlob blob, Stream stream, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			stream.Position = 0; // Rewind the stream. IMPORTANT!

			var blobExits = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
			var accessCondition = (string.IsNullOrEmpty(leaseId) ? null : new AccessCondition { LeaseId = leaseId });

			if (blob is CloudAppendBlob)
			{
				var appendBlob = blob as CloudAppendBlob;
				if (!blobExits)
				{
					await appendBlob.CreateOrReplaceAsync(accessCondition, null, null, cancellationToken).ConfigureAwait(false);
					blobExits = true;
				}
				await appendBlob.AppendFromStreamAsync(stream, accessCondition, null, null, cancellationToken);
			}
			else if (!blobExits)
			{
				await blob.UploadFromStreamAsync(stream, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
			}
			else
			{
				var content = new MemoryStream();
				await blob.DownloadToStreamAsync(content, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
				await stream.CopyToAsync(content).ConfigureAwait(false);
				await blob.UploadFromStreamAsync(content, accessCondition, null, null, cancellationToken).ConfigureAwait(false);
			}
		}

		public static Task AppendBytesAsync(this ICloudBlob blob, byte[] buffer, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var stream = new MemoryStream(buffer);
			return blob.AppendStreamAsync(stream, leaseId, cancellationToken);
		}

		public static Task AppendTextAsync(this ICloudBlob blob, string content, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			var buffer = Encoding.UTF8.GetBytes(content);
			return blob.AppendBytesAsync(buffer, leaseId, cancellationToken);
		}

		public static Task SetMetadataAsync(this ICloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var accessCondition = new AccessCondition();
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessCondition.LeaseId = leaseId;
			}
			return blob.SetMetadataAsync(accessCondition, null, null, cancellationToken);
		}

		public static async Task<string> DownloadTextAsync(this ICloudBlob blob, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var stream = new MemoryStream())
			{
				await blob.DownloadToStreamAsync(stream, cancellationToken).ConfigureAwait(false);
				using (var reader = new StreamReader(stream, true))
				{
					stream.Position = 0;
					return await reader.ReadToEndAsync();
				}
			}
		}

		public static async Task<byte[]> DownloadByteArrayAsync(this ICloudBlob blob, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (var ms = new MemoryStream())
			{
				await blob.DownloadToStreamAsync(ms, cancellationToken).ConfigureAwait(false);
				ms.Position = 0;
				return ms.ToArray();
			}
		}

		public static async Task CopyAsync(this ICloudBlob blob, string destinationBlobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			var container = blob.Container;
			Func<CopyState, Task> waitForCopyCompletion = async (copyState) =>
			{
				while (copyState.Status == CopyStatus.Pending)
					await Task.Delay(100).ConfigureAwait(false);
				if (copyState.Status != CopyStatus.Success)
					throw new ApplicationException($"CopyAsync failed: {copyState.Status}");
			};

			if (blob is CloudBlockBlob)
			{
				var blockTarget = container.GetBlockBlobReference(destinationBlobName);
				await blockTarget.StartCopyAsync((CloudBlockBlob)blob, cancellationToken).ConfigureAwait(false);
				await waitForCopyCompletion(blockTarget.CopyState).ConfigureAwait(false);
			}
			else if (blob is CloudPageBlob)
			{
				var pageTarget = container.GetPageBlobReference(destinationBlobName);
				await pageTarget.StartCopyAsync((CloudPageBlob)blob, cancellationToken).ConfigureAwait(false);
				await waitForCopyCompletion(pageTarget.CopyState).ConfigureAwait(false);
			}
			else
			{
				var appendTarget = container.GetAppendBlobReference(destinationBlobName);
				await appendTarget.StartCopyAsync((CloudAppendBlob)blob, cancellationToken).ConfigureAwait(false);
				await waitForCopyCompletion(appendTarget.CopyState).ConfigureAwait(false);
			}

		}

		/// <summary>
		/// Creates a Shared Access Signature URI for the blob.
		/// </summary>
		/// <remarks>
		/// Inspired by http://gauravmantri.com/2013/02/13/revisiting-windows-azure-shared-access-signature/
		/// </remarks>
		public static string GetSharedAccessSignatureUri(this ICloudBlob blob, SharedAccessBlobPermissions permission, ISystemClock systemClock = null)
		{
			return GetSharedAccessSignatureUri(blob, permission, TimeSpan.FromMinutes(15), systemClock);
		}

		public static string GetSharedAccessSignatureUri(this ICloudBlob blob, SharedAccessBlobPermissions permission, TimeSpan duration, ISystemClock systemClock = null)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var now = (systemClock ?? SystemClock.Instance).UtcNow;
			var sas = blob.GetSharedAccessSignature(new SharedAccessBlobPolicy
			{
				Permissions = permission,
				SharedAccessStartTime = now.AddMinutes(-5), // Start time is back by 5 minutes to take clock skewness into consideration
				SharedAccessExpiryTime = now.Add(duration)
			});
			return string.Format(CultureInfo.InvariantCulture, "{0}{1}", blob.Uri, sas);
		}

		#endregion
	}
}
