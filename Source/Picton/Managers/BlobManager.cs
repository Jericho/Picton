using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Picton.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	public class BlobManager : IBlobManager
	{
		#region FIELDS

		private const string PATH_SEPARATOR = "/";
		private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
		private static readonly IRetryPolicy _retryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(1), 3);

		private readonly IStorageAccount _storageAccount;
		private readonly string _containerName;
		private readonly CloudBlobClient _blobClient;
		private readonly CloudBlobContainer _blobContainer;

		#endregion

		#region CONSTRUCTORS

#if NETFULL
		[ExcludeFromCodeCoverage]
#endif
		public BlobManager(string containerName, CloudStorageAccount cloudStorageAccount, BlobContainerPublicAccessType accessType = BlobContainerPublicAccessType.Off)
			: this(containerName, StorageAccount.FromCloudStorageAccount(cloudStorageAccount), accessType)
		{ }

		public BlobManager(string containerName, IStorageAccount storageAccount, BlobContainerPublicAccessType accessType = BlobContainerPublicAccessType.Off)
		{
			if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentNullException(nameof(containerName));
			if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));

			_storageAccount = storageAccount;
			_containerName = containerName;
			_blobClient = _storageAccount.CreateCloudBlobClient();
			_blobContainer = _blobClient.GetContainerReference(_containerName);

			_blobContainer.CreateIfNotExistsAsync(accessType, null, null, CancellationToken.None).Wait();
		}

		#endregion

		#region PUBLIC METHODS

		public async Task<CloudBlob> GetBlobReferenceAsync(string blobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanBlobName = SanitizeBlobName(blobName);
			var source = _blobContainer.GetBlobReference(cleanBlobName);

			if (!await source.ExistsAsync(null, null, cancellationToken).ConfigureAwait(false)) return null;

			switch (source.BlobType)
			{
				case BlobType.AppendBlob:
					return _blobContainer.GetAppendBlobReference(cleanBlobName);
				case BlobType.BlockBlob:
					return _blobContainer.GetBlockBlobReference(cleanBlobName);
				case BlobType.PageBlob:
					return _blobContainer.GetPageBlobReference(cleanBlobName);
				default:
					throw new Exception($"Unknow blob type: {source.BlobType}");
			}
		}

		public async Task<BlobProperties> GetBlobContentAsync(string blobName, Stream outputStream, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = _blobContainer.GetBlobReference(cleanBlobName);
			var exists = await blob.ExistsAsync(null, null, cancellationToken).ConfigureAwait(false);
			if (!exists) return null;

			await blob.DownloadToStreamAsync(outputStream, null, null, null, cancellationToken).ConfigureAwait(false);
			return blob.Properties;
		}

		public async Task<byte[]> GetBlobContentAsync(string blobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = _blobContainer.GetBlobReference(cleanBlobName);
			var exists = await blob.ExistsAsync(null, null, cancellationToken).ConfigureAwait(false);
			if (!exists) return null;

			byte[] buffer;
			using (var ms = new MemoryStream())
			{
				await blob.DownloadToStreamAsync(ms, null, null, null, cancellationToken).ConfigureAwait(false);
				buffer = ms.ToArray();
			}

			return buffer;
		}

		public async Task UploadStreamAsync(string blobName, Stream stream, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = await GetBlobReferenceAsync(cleanBlobName, cancellationToken).ConfigureAwait(false);

			var leaseId = string.Empty;
			if (acquireLease && blob != null)
			{
				maxLeaseAttempts = Math.Max(maxLeaseAttempts, 1);   // At least one attempt
				maxLeaseAttempts = Math.Min(maxLeaseAttempts, 10);  // No more than 10 attempts
				for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
				{
					leaseId = await blob.TryAcquireLeaseAsync(null, maxLeaseAttempts, cancellationToken);
					if (string.IsNullOrEmpty(leaseId)) break;
					else if (attempts + 1 < maxLeaseAttempts) await Task.Delay(500);    // Make sure we don't attempt too quickly
				}

				if (string.IsNullOrEmpty(leaseId)) throw new Exception("Unable to obtain blob lease");
			}

			if (blob == null)
			{
				// We make the assumption that new blobs should be of type 'block blob'
				blob = _blobContainer.GetBlockBlobReference(cleanBlobName);
			}

			blob.Properties.ContentType = mimeType ?? MimeTypeMap.GetMimeType(Path.GetExtension(cleanBlobName));

			if (!string.IsNullOrEmpty(cacheControl)) blob.Properties.CacheControl = cacheControl;
			if (!string.IsNullOrEmpty(contentEncoding)) blob.Properties.ContentEncoding = contentEncoding;

			await blob.UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			if (metadata != null)
			{
				foreach (var key in metadata.AllKeys)
				{
					blob.Metadata[key] = metadata[key];
				}

				await blob.SetMetadataAsync(leaseId, cancellationToken);
			}

			if (!string.IsNullOrEmpty(leaseId)) await blob.ReleaseLeaseAsync(new AccessCondition() { LeaseId = leaseId }, null, null, cancellationToken).ConfigureAwait(false);
		}

		public Task UploadBytesAsync(string blobName, byte[] buffer, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var memoryStream = new MemoryStream(buffer);
			return this.UploadStreamAsync(blobName, memoryStream, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public Task UploadTextAsync(string blobName, string content, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var buffer = content.ToBytes();
			return this.UploadBytesAsync(blobName, buffer, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public Task UploadFileAsync(string blobName, string fileName, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var fileStream = File.OpenRead(fileName);
			return this.UploadStreamAsync(blobName, fileStream, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public async Task AppendStreamAsync(string blobName, Stream stream, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = await GetBlobReferenceAsync(cleanBlobName, cancellationToken).ConfigureAwait(false);

			var leaseId = string.Empty;
			if (acquireLease && blob != null)
			{
				maxLeaseAttempts = Math.Max(maxLeaseAttempts, 1);   // At least one attempt
				maxLeaseAttempts = Math.Min(maxLeaseAttempts, 10);  // No more than 10 attempts
				for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
				{
					leaseId = await blob.TryAcquireLeaseAsync(null, maxLeaseAttempts, cancellationToken);
					if (string.IsNullOrEmpty(leaseId)) break;
					else if (attempts + 1 < maxLeaseAttempts) await Task.Delay(500);    // Make sure we don't attempt too quickly
				}

				if (string.IsNullOrEmpty(leaseId)) throw new Exception("Unable to obtain blob lease");
			}

			if (blob == null)
			{
				// We make the assumption that new blobs should be of type 'block blob'
				blob = _blobContainer.GetBlockBlobReference(cleanBlobName);
			}

			blob.Properties.ContentType = mimeType ?? MimeTypeMap.GetMimeType(Path.GetExtension(cleanBlobName));

			if (!string.IsNullOrEmpty(cacheControl)) blob.Properties.CacheControl = cacheControl;
			if (!string.IsNullOrEmpty(contentEncoding)) blob.Properties.ContentEncoding = contentEncoding;

			await blob.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			if (metadata != null)
			{
				foreach (var key in metadata.AllKeys)
				{
					blob.Metadata[key] = metadata[key];
				}

				await blob.SetMetadataAsync(leaseId, cancellationToken);
			}

			if (!string.IsNullOrEmpty(leaseId)) await blob.ReleaseLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);
		}

		public Task AppendBytesAsync(string blobName, byte[] buffer, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var memoryStream = new MemoryStream(buffer);
			return AppendStreamAsync(blobName, memoryStream, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public Task AppendTextAsync(string blobName, string content, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var buffer = content.ToBytes();
			return this.AppendBytesAsync(blobName, buffer, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public async Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = await GetBlobReferenceAsync(cleanBlobName, cancellationToken).ConfigureAwait(false);
			await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null, cancellationToken).ConfigureAwait(false);
		}

		public async Task DeleteBlobsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default(CancellationToken))
		{
			var blobItems = await ListBlobsAsync(prefix, true, false).ConfigureAwait(false);
			if (blobItems != null)
			{
				foreach (var appendBlob in blobItems.OfType<CloudAppendBlob>())
				{
					await appendBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null, cancellationToken).ConfigureAwait(false);
				}

				foreach (var blockBlob in blobItems.OfType<CloudBlockBlob>())
				{
					await blockBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null, cancellationToken).ConfigureAwait(false);
				}

				foreach (var pageBlob in blobItems.OfType<CloudPageBlob>())
				{
					await pageBlob.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots, null, null, null, cancellationToken).ConfigureAwait(false);
				}
			}
		}

		public Task<IEnumerable<IListBlobItem>> ListBlobsAsync(string prefix, bool includeSubFolders = false, bool includeMetadata = false, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanPrefix = SanitizeBlobName(prefix, true);
			var blobPrefix = $"{_containerName}{PATH_SEPARATOR}{cleanPrefix}";
			var listingDetails = includeMetadata ? BlobListingDetails.Metadata : BlobListingDetails.None;

			return _blobContainer.ListBlobsAsync(blobPrefix, includeSubFolders, listingDetails, maxResults, cancellationToken);
		}

		public Task<IEnumerable<CloudBlobDirectory>> ListSubFoldersAsync(string folder, bool includeMetadata = false, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanFolder = SanitizeBlobName(folder, true);
			var listingDetails = includeMetadata ? BlobListingDetails.Metadata : BlobListingDetails.None;
			return _blobContainer.ListSubFoldersAsync(cleanFolder, listingDetails, maxResults, cancellationToken);
		}

		public Task CopyBlobAsync(string sourceBlobName, string destinationBlobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			return MoveOrCopyBlobAsync(sourceBlobName, destinationBlobName, false, cancellationToken);
		}

		public Task MoveBlobAsync(string sourceBlobName, string destinationBlobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			return MoveOrCopyBlobAsync(sourceBlobName, destinationBlobName, true, cancellationToken);
		}

		#endregion

		#region PRIVATE METHODS

		private async Task MoveOrCopyBlobAsync(string sourceBlobName, string destinationBlobName, bool deleteSourceAfterCopy, CancellationToken cancellationToken = default(CancellationToken))
		{
			var cleanSourceName = SanitizeBlobName(sourceBlobName);
			var cleanDestinationName = SanitizeBlobName(destinationBlobName);

			if (cleanSourceName == cleanDestinationName) return;

			var source = _blobContainer.GetBlobReference(cleanSourceName);
			if (!await source.ExistsAsync(null, null, cancellationToken).ConfigureAwait(false)) return;

			var blob = await GetBlobReferenceAsync(cleanSourceName, cancellationToken).ConfigureAwait(false);
			await blob.CopyAsync(cleanDestinationName, cancellationToken).ConfigureAwait(false);

			if (deleteSourceAfterCopy) await source.DeleteAsync().ConfigureAwait(false);
		}

		private string SanitizeBlobName(string blobName, bool allowEmptyName = false)
		{
			blobName = blobName?
				.Replace(@"\", PATH_SEPARATOR) // Azure uses forward slash as the path segment seperator
				.Replace(" ", "_") // Azure supports spaces but it leads to problems in URLs
				.Replace("#", "_") // Azure supports the # character but it leads to problems in URLs
				.Replace("'", "_") // Azure supports quotes but it leads to problems in URLs
				.TrimStart($"{PATH_SEPARATOR}devstoreaccount1")
				.TrimStart($"{PATH_SEPARATOR}{_containerName}")
				.TrimStart(PATH_SEPARATOR);

			if (!allowEmptyName && string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException("Name cannot be empty");
			if (blobName.Length > 1024) throw new ArgumentException("Name cannot be more than 1,024 characters long");

			return blobName;
		}

		#endregion
	}
}
