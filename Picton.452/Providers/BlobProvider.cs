using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using Picton.Extensions;
using Picton.Interfaces;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Providers
{
	public class BlobProvider
	{
		#region FIELDS

		private readonly IStorageAccount _storageAccount;
		private readonly string _containerName;
		private readonly IBlobClient _blobClient;
		private readonly CloudBlobContainer _blobContainer;

		private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);
		private static readonly IRetryPolicy _retryPolicy = new ExponentialRetry(TimeSpan.FromSeconds(1), 3);
		private const string PATH_SEPARATOR = "/";

		#endregion

		#region CONSTRUCTORS

		[ExcludeFromCodeCoverage]
		public BlobProvider(string containerName, CloudStorageAccount cloudStorageAccount, BlobContainerPublicAccessType accessType = BlobContainerPublicAccessType.Off) :
			this(containerName, StorageAccount.FromCloudStorageAccount(cloudStorageAccount), accessType)
		{ }

		public BlobProvider(string containerName, IStorageAccount storageAccount, BlobContainerPublicAccessType accessType = BlobContainerPublicAccessType.Off)
		{
			if (string.IsNullOrWhiteSpace(containerName)) throw new ArgumentNullException(nameof(containerName));
			if (storageAccount == null) throw new ArgumentNullException(nameof(storageAccount));

			_storageAccount = storageAccount;
			_containerName = containerName;
			_blobClient = _storageAccount.CreateCloudBlobClient();
			_blobContainer = _blobClient.GetContainerReference(_containerName);

			var tasks = new List<Task>();
			tasks.Add(_blobContainer.CreateIfNotExistsAsync(accessType, null, null, CancellationToken.None));
			Task.WaitAll(tasks.ToArray());
		}

		#endregion

		#region PUBLIC METHODS

		public async Task<BlobProperties> GetBlobContentAsync(string blobName, Stream outputStream, CancellationToken cancellationToken = default(CancellationToken))
		{
			var blob = _blobContainer.GetBlockBlobReference(blobName);
			var exists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
			if (!exists) return null;

			await blob.DownloadToStreamAsync(outputStream, cancellationToken).ConfigureAwait(false);
			return blob.Properties;
		}

		public async Task<byte[]> GetBlobContentAsync(string blobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			var blob = await _blobContainer.GetBlobReferenceFromServerAsync(blobName, cancellationToken);

			byte[] buffer;
			using (var ms = new MemoryStream())
			{
				await blob.DownloadToStreamAsync(ms, cancellationToken).ConfigureAwait(false);
				buffer = ms.ToArray();
			}

			return buffer;
		}

		public async Task UploadStreamAsync(string blobName, Stream stream, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var blob = _blobContainer.GetBlockBlobReference(blobName);

			var leaseId = "";
			if (acquireLease && await blob.ExistsAsync())
			{
				maxLeaseAttempts = Math.Max(maxLeaseAttempts, 1);   // At least one attempt
				maxLeaseAttempts = Math.Min(maxLeaseAttempts, 10);  // No more than 10 attempts
				for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
				{
					leaseId = await blob.TryAcquireLeaseAsync(cancellationToken);
					if (string.IsNullOrEmpty(leaseId)) break;
					else if (attempts + 1 < maxLeaseAttempts) await Task.Delay(500);    // Make sure we don't attempt too quickly
				}

				if (string.IsNullOrEmpty(leaseId)) throw new Exception("Unable to obtain blob lease");
			}

			blob.Properties.ContentType = mimeType ?? MimeTypeMap.GetMimeType(Path.GetExtension(blobName));

			if (string.IsNullOrEmpty(cacheControl)) blob.Properties.CacheControl = cacheControl;
			if (string.IsNullOrEmpty(contentEncoding)) blob.Properties.ContentEncoding = contentEncoding;

			await blob.UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			if (metadata != null && metadata.Count > 0)
			{
				Array.ForEach(metadata.AllKeys, key => blob.Metadata[key] = metadata[key]);
				await blob.SetMetadataAsync(leaseId, cancellationToken);
			}

			if (string.IsNullOrEmpty(leaseId)) await blob.ReleaseLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);
		}

		public Task UploadBytesAsync(string blobName, byte[] buffer, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var memoryStream = new MemoryStream(buffer);
			return this.UploadStreamAsync(blobName, memoryStream, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public Task UploadTextAsync(string blobName, string content, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			return this.UploadBytesAsync(blobName, Encoding.UTF8.GetBytes(content), mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public Task UploadFileAsync(string blobName, string fileName, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var fileStream = File.OpenRead(fileName);
			return this.UploadStreamAsync(blobName, fileStream, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public async Task AppendStreamAsync(string blobName, Stream stream, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			var currentContent = new MemoryStream();
			var properties = await GetBlobContentAsync(blobName, currentContent, cancellationToken).ConfigureAwait(false);

			var newContent = new MultiStream();
			newContent.AddStream(currentContent);
			newContent.AddStream(stream);

			await UploadStreamAsync(blobName, newContent, properties.ContentType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken).ConfigureAwait(false);
		}

		public Task AppendBytesAsync(string blobName, byte[] buffer, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			return AppendStreamAsync(blobName, new MemoryStream(buffer), metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public Task AppendTextAsync(string blobName, string content, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken))
		{
			return AppendBytesAsync(blobName, content.ToBytes(), metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		public Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			blobName = blobName
				.TrimStart($"{PATH_SEPARATOR}devstoreaccount1")
				.TrimStart($"{PATH_SEPARATOR}{_containerName}")
				.TrimStart(PATH_SEPARATOR);
			var blob = _blobContainer.GetBlockBlobReference(blobName);
			return blob.DeleteIfExistsAsync(cancellationToken);
		}

		public async Task DeleteBlobsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default(CancellationToken))
		{
			var blobItems = ListBlobs(prefix, false);
			if (blobItems != null)
			{
				foreach (var blockBlob in blobItems.OfType<CloudBlockBlob>())
				{
					await blockBlob.DeleteIfExistsAsync(cancellationToken);
				}
				foreach (var pageBlob in blobItems.OfType<CloudPageBlob>())
				{
					await pageBlob.DeleteIfExistsAsync(cancellationToken);
				}
			}
		}

		public IEnumerable<IListBlobItem> ListBlobs(string folder, bool includeSubFolders = false, bool includeMetadata = false, int? maxResults = null)
		{
			var blobPrefix = $"{_containerName}{PATH_SEPARATOR}{folder}";
			var listingDetails = (includeMetadata ? BlobListingDetails.Metadata : BlobListingDetails.None);
			if (maxResults.HasValue)
			{
				var segmentedResult = _blobClient.ListBlobsSegmentedAsync(blobPrefix, includeSubFolders, listingDetails, maxResults).Result;
				return segmentedResult.Results;
			}
			else
			{
				return _blobClient.ListBlobs(blobPrefix, includeSubFolders, listingDetails);
			}
		}

		public IEnumerable<CloudBlobDirectory> ListSubFolders(string folder)
		{
			if (string.IsNullOrEmpty(folder)) return _blobContainer.ListBlobs().OfType<CloudBlobDirectory>();
			else return _blobContainer.GetDirectoryReference(folder).ListBlobs().OfType<CloudBlobDirectory>();
		}

		public ICloudBlob GetBlob(string blobName)
		{
			return _blobContainer.GetBlockBlobReference(blobName);
		}

		public Task CopyBlobAsync(string sourceBlobName, string destinationBlobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			return MoveOrCopyBlobAsync(sourceBlobName, destinationBlobName, false, cancellationToken);
		}

		public Task MoveBlobAsync(string sourceBlobName, string destinationBlobName, CancellationToken cancellationToken = default(CancellationToken))
		{
			return MoveOrCopyBlobAsync(sourceBlobName, destinationBlobName, true, cancellationToken);
		}

		public string GetNewSharedAccessSignature(string fileName, TimeSpan duration)
		{
			var signatureUri = GetBlob(fileName).GetSharedAccessSignatureUri(SharedAccessBlobPermissions.Read, duration);
			return signatureUri;
		}

		public Task<bool> ContainerExists(CancellationToken cancellationToken = default(CancellationToken))
		{
			return _blobContainer.ExistsAsync(cancellationToken);
		}

		#endregion

		#region STATIC METHODS

		public static string SanitizeBlobName(string blobName)
		{
			blobName = blobName?
				.Replace(@"\", "/") // Azure uses forward slash as the path segment seperator
				.Replace(" ", "_")  // Azure supports spaces but it leads to problems in URLs
				.Replace("#", "_")  // Azure supports the # character but it leads to problems in URLs
				.Replace("'", "_"); // Azure supports quotes but it leads to problems in URLs

			if (string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException("A blob name must be at least one character long", nameof(blobName));
			if (blobName.Length > 1024) throw new ArgumentException("A blob name cannot be more than 1,024 characters long", nameof(blobName));

			return blobName;
		}

		#endregion

		#region PRIVATE METHODS

		private async Task MoveOrCopyBlobAsync(string sourceBlobName, string destinationBlobName, bool deleteSource, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (sourceBlobName == destinationBlobName) return;

			var source = (CloudBlockBlob)await _blobContainer.GetBlobReferenceFromServerAsync(sourceBlobName).ConfigureAwait(false);
			var target = _blobContainer.GetBlockBlobReference(destinationBlobName);

			await target.StartCopyAsync(source, cancellationToken).ConfigureAwait(false);

			while (target.CopyState.Status == CopyStatus.Pending)
				await Task.Delay(100);

			if (target.CopyState.Status != CopyStatus.Success)
				throw new ApplicationException($"Move or Copy failed: {target.CopyState.Status}");

			if (deleteSource) await source.DeleteAsync();
		}

		#endregion
	}
}
