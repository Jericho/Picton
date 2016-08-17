using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Extensions
{
	public static class CloudBlobExtensions
	{
		#region PUBLIC EXTENSION METHODS

		public static async Task<string> TryAcquireLeaseAsync(this ICloudBlob blob, TimeSpan? leaseTime = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			try { return await blob.AcquireLeaseAsync(leaseTime, cancellationToken); }
			catch (WebException e)
			{
				if ((e.Response == null) || ((HttpWebResponse)e.Response).StatusCode != HttpStatusCode.Conflict) // 409, already leased
				{
					throw;
				}
				e.Response.Close();
				return null;
			}
		}

		public static async Task<string> AcquireLeaseAsync(this ICloudBlob blob, TimeSpan? leaseTime = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			// From: https://msdn.microsoft.com/en-us/library/azure/ee691972.aspx
			// The Lease Blob operation establishes and manages a lock on a blob for write and delete operations.
			// The lock duration can be 15 to 60 seconds, or can be infinite. 
			if (leaseTime.HasValue && (leaseTime.Value < TimeSpan.FromSeconds(15) | leaseTime.Value > TimeSpan.FromSeconds(60)))
			{
				throw new ArgumentOutOfRangeException(nameof(leaseTime), "LeaseTime must be between 15 and 60 seconds");
			}

			var defaultLeaseTime = TimeSpan.FromSeconds(15);    // Acquire a 15 second lease on the blob. Leave it null for infinite lease. Otherwise it should be between 15 and 60 seconds.
			var proposedLeaseId = (string)null;                 // Proposed lease id (leave it null for storage service to return you one).
			var leaseId = await blob.AcquireLeaseAsync(leaseTime.GetValueOrDefault(defaultLeaseTime), proposedLeaseId, cancellationToken);
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

		public static Task UploadStreamAsync(this ICloudBlob blob, Stream stream, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (stream == null) throw new ArgumentNullException(nameof(stream));

			stream.Position = 0; // Rewind the stream. IMPORTANT!

			if (string.IsNullOrEmpty(leaseId))
			{
				return blob.UploadFromStreamAsync(stream, cancellationToken);
			}
			else
			{
				var accessCondition = new AccessCondition { LeaseId = leaseId };
				return blob.UploadFromStreamAsync(stream, accessCondition, null, null, cancellationToken);
			}
		}

		public static Task SetMetadataAsync(this ICloudBlob blob, string leaseId, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			if (string.IsNullOrEmpty(leaseId))
			{
				return blob.SetMetadataAsync(cancellationToken);
			}
			else
			{
				var accessCondition = new AccessCondition { LeaseId = leaseId };
				return blob.SetMetadataAsync(accessCondition, null, null, cancellationToken);
			}
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

		public static async Task UploadTextAsync(this ICloudBlob blob, string text, CancellationToken cancellationToken = default(CancellationToken))
		{
			using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
			{
				await blob.UploadFromStreamAsync(ms, cancellationToken).ConfigureAwait(false);
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
