using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Picton.Interfaces;
using Picton.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	public class BlobManager : IBlobManager
	{
		#region FIELDS

		private const string PATH_SEPARATOR = "/";

		private readonly BlobContainerClient _blobContainer;

		#endregion

		#region CONSTRUCTORS

		[ExcludeFromCodeCoverage]
		public BlobManager(string connectionString, string containerName, PublicAccessType accessType = PublicAccessType.None)
		{
			_blobContainer = new BlobContainerClient(connectionString, containerName);
			_blobContainer.CreateIfNotExists(accessType);
		}

		public BlobManager(BlobContainerClient blobContainer, PublicAccessType accessType = PublicAccessType.None)
		{
			_blobContainer = blobContainer;
			_blobContainer.CreateIfNotExists(accessType);
		}

		#endregion

		#region PUBLIC METHODS

		/// <inheritdoc/>
		public BlobClient GetBlobReference(string blobName)
		{
			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = _blobContainer.GetBlobClient(cleanBlobName);

			return blob;
		}

		/// <inheritdoc/>
		public async Task<BlobDownloadInfo> GetBlobContentAsync(string blobName, CancellationToken cancellationToken = default)
		{
			try
			{
				var cleanBlobName = SanitizeBlobName(blobName);
				var blob = _blobContainer.GetBlobClient(cleanBlobName);

				var response = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
				return response.Value;
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				return null;
			}
		}

		/// <inheritdoc/>
		public async Task<byte[]> GetBlobBinaryContentAsync(string blobName, CancellationToken cancellationToken = default)
		{
			try
			{
				var cleanBlobName = SanitizeBlobName(blobName);
				var blob = _blobContainer.GetBlobClient(cleanBlobName);

				var buffer = await blob.DownloadByteArrayAsync(cancellationToken).ConfigureAwait(false);
				return buffer;
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				return null;
			}
		}

		public async Task UploadStreamAsync(string blobName, Stream stream, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(blobName)) throw new ArgumentException("You must specify the name of the blob", nameof(blobName));
			if (stream == null) throw new ArgumentNullException(nameof(stream));
			if (maxLeaseAttempts < 1 || maxLeaseAttempts > 10) throw new ArgumentOutOfRangeException(nameof(maxLeaseAttempts), "Number of attempts must be between 1 and 10");

			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = _blobContainer.GetBlobClient(cleanBlobName);

			var leaseId = string.Empty;
			if (acquireLease)
			{
				for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
				{
					leaseId = await blob.TryAcquireLeaseAsync(null, maxLeaseAttempts, cancellationToken).ConfigureAwait(false);
					if (string.IsNullOrEmpty(leaseId)) break;
					else if (attempts + 1 < maxLeaseAttempts) await Task.Delay(500, cancellationToken).ConfigureAwait(false);    // Make sure we don't attempt too quickly
				}

				if (string.IsNullOrEmpty(leaseId)) throw new Exception("Unable to obtain blob lease");
			}

			await blob.UploadStreamAsync(stream, mimeType, cacheControl, contentEncoding, leaseId, cancellationToken).ConfigureAwait(false);

			if (metadata != null)
			{
				await blob.SetMetadataAsync(metadata, leaseId, cancellationToken).ConfigureAwait(false);
			}

			if (!string.IsNullOrEmpty(leaseId)) await blob.ReleaseLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public Task UploadBytesAsync(string blobName, byte[] buffer, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			var memorystream = new MemoryStream(buffer);
			return this.UploadStreamAsync(blobName, memorystream, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		/// <inheritdoc/>
		public Task UploadTextAsync(string blobName, string content, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			var buffer = content.ToBytes();
			return this.UploadBytesAsync(blobName, buffer, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		/// <inheritdoc/>
		public Task UploadFileAsync(string blobName, string fileName, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			var fileStream = File.OpenRead(fileName);
			return this.UploadStreamAsync(blobName, fileStream, mimeType, metadata, cacheControl, contentEncoding, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task AppendStreamAsync(string blobName, Stream stream, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			if (string.IsNullOrEmpty(blobName)) throw new ArgumentException("You must specify the name of the blob", nameof(blobName));
			if (stream == null) throw new ArgumentNullException(nameof(stream));
			if (maxLeaseAttempts < 1 || maxLeaseAttempts > 10) throw new ArgumentOutOfRangeException(nameof(maxLeaseAttempts), "Number of attempts must be between 1 and 10");

			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = _blobContainer.GetBlobClient(cleanBlobName);

			var leaseId = string.Empty;
			if (acquireLease)
			{
				for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
				{
					leaseId = await blob.TryAcquireLeaseAsync(null, maxLeaseAttempts, cancellationToken).ConfigureAwait(false);
					if (string.IsNullOrEmpty(leaseId)) break;
					else if (attempts + 1 < maxLeaseAttempts) await Task.Delay(500, cancellationToken).ConfigureAwait(false);    // Make sure we don't attempt too quickly
				}

				if (string.IsNullOrEmpty(leaseId)) throw new Exception("Unable to obtain blob lease");
			}

			await blob.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			if (metadata != null)
			{
				await blob.SetMetadataAsync(metadata, leaseId, cancellationToken).ConfigureAwait(false);
			}

			if (!string.IsNullOrEmpty(leaseId)) await blob.ReleaseLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public Task AppendBytesAsync(string blobName, byte[] buffer, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			var memoryStream = new MemoryStream(buffer);
			return AppendStreamAsync(blobName, memoryStream, metadata, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		/// <inheritdoc/>
		public Task AppendTextAsync(string blobName, string content, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			var buffer = content.ToBytes();
			return this.AppendBytesAsync(blobName, buffer, metadata, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		/// <inheritdoc/>
		public async Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default)
		{
			var cleanBlobName = SanitizeBlobName(blobName);
			var blob = _blobContainer.GetBlobClient(cleanBlobName);
			await blob.DeleteAsync(DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken).ConfigureAwait(false);
		}

		/// <inheritdoc/>
		public async Task DeleteBlobsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default)
		{
			var blobItems = ListBlobs(prefix, true, cancellationToken);
			foreach (var blob in blobItems)
			{
				await _blobContainer.DeleteBlobAsync(blob.Name, DeleteSnapshotsOption.IncludeSnapshots, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <inheritdoc/>
		public Pageable<BlobItem> ListBlobs(string prefix, bool includeMetadata = false, CancellationToken cancellationToken = default)
		{
			var cleanPrefix = SanitizeBlobName(prefix, true);
			var traits = includeMetadata ? BlobTraits.Metadata : BlobTraits.None;

			return _blobContainer.GetBlobs(traits, BlobStates.None, cleanPrefix, cancellationToken);
		}

		/// <inheritdoc/>
		public Task CopyBlobAsync(string sourceBlobName, string destinationBlobName, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			return MoveOrCopyBlobAsync(sourceBlobName, destinationBlobName, false, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		/// <inheritdoc/>
		public Task MoveBlobAsync(string sourceBlobName, string destinationBlobName, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			return MoveOrCopyBlobAsync(sourceBlobName, destinationBlobName, true, acquireLease, maxLeaseAttempts, cancellationToken);
		}

		#endregion

		#region PRIVATE METHODS

		private async Task MoveOrCopyBlobAsync(string sourceBlobName, string destinationBlobName, bool deleteSourceAfterCopy, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			var cleanSourceName = SanitizeBlobName(sourceBlobName);
			var cleanDestinationName = SanitizeBlobName(destinationBlobName);

			if (cleanSourceName == cleanDestinationName) return;

			var blob = _blobContainer.GetBlobClient(cleanSourceName);

			var leaseId = string.Empty;
			if (acquireLease)
			{
				for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
				{
					leaseId = await blob.TryAcquireLeaseAsync(null, maxLeaseAttempts, cancellationToken).ConfigureAwait(false);
					if (string.IsNullOrEmpty(leaseId)) break;
					else if (attempts + 1 < maxLeaseAttempts) await Task.Delay(500, cancellationToken).ConfigureAwait(false);    // Make sure we don't attempt too quickly
				}

				if (string.IsNullOrEmpty(leaseId)) throw new Exception("Unable to obtain blob lease");
			}
			else
			{
				await blob.CreateIfNotExistsAsync(null, null, null, null, null, cancellationToken).ConfigureAwait(false);
			}

			await _blobContainer.CopyAsync(cleanSourceName, cleanDestinationName, leaseId, true, cancellationToken).ConfigureAwait(false);

			if (deleteSourceAfterCopy) await blob.DeleteAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
		}

		private string SanitizeBlobName(string blobName, bool allowEmptyName = false)
		{
			blobName = blobName?
				.Replace(@"\", PATH_SEPARATOR) // Azure uses forward slash as the path segment seperator
				.Replace(" ", "_") // Azure supports spaces but it leads to problems in URLs
				.Replace("#", "_") // Azure supports the # character but it leads to problems in URLs
				.Replace("'", "_") // Azure supports quotes but it leads to problems in URLs
				.TrimStart($"{PATH_SEPARATOR}devstoreaccount1")
				.TrimStart($"{PATH_SEPARATOR}{_blobContainer.Name}")
				.TrimStart(PATH_SEPARATOR);

			if (!allowEmptyName && string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException("Name cannot be empty");
			if (blobName.Length > 1024) throw new ArgumentException("Name cannot be more than 1,024 characters long");

			return blobName;
		}

		#endregion
	}
}
