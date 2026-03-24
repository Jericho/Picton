using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Picton.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	/// <inheritdoc/>
	public class BlobManager : IBlobManager
	{
		#region FIELDS

		private readonly BlobContainerClient _blobContainer;

		#endregion

		#region CONSTRUCTORS

		/// <summary>
		/// Initializes a new instance of the <see cref="BlobManager"/> class for managing blobs in the specified container.
		/// </summary>
		/// <remarks>If the specified container does not exist, it is created with the specified public access level.
		/// This constructor does not validate the existence of the storage account or the validity of the connection
		/// string.</remarks>
		/// <param name="connectionString">The connection string used to authenticate and connect to the Azure Blob Storage account.</param>
		/// <param name="containerName">The name of the blob container to manage. If the container does not exist, it will be created.</param>
		/// <param name="accessType">The level of public access to grant to the container if it is created. The default is None.</param>
		[ExcludeFromCodeCoverage]
		public BlobManager(string connectionString, string containerName, PublicAccessType accessType = PublicAccessType.None)
		{
			_blobContainer = new BlobContainerClient(connectionString, containerName);
			_blobContainer.CreateIfNotExists(accessType);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="BlobManager"/> class using the specified blob container client and access type.
		/// </summary>
		/// <remarks>If the specified blob container does not exist, it is created with the provided access type. If
		/// the container already exists, its access level is not modified.</remarks>
		/// <param name="blobContainer">The BlobContainerClient instance used to interact with the underlying blob container. Cannot be null.</param>
		/// <param name="accessType">The level of public access to apply to the container if it is created. The default is PublicAccessType.None.</param>
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

		/// <inheritdoc/>
		public async Task UploadStreamAsync(string blobName, Stream stream, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
		{
			ArgumentNullException.ThrowIfNullOrEmpty(blobName, nameof(blobName), "You must specify the name of the blob");
			ArgumentNullException.ThrowIfNull(stream);

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
			ArgumentNullException.ThrowIfNullOrEmpty(blobName, nameof(blobName), "You must specify the name of the blob");
			ArgumentNullException.ThrowIfNull(stream);

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
				.Replace(@"\", "/") // Azure uses forward slash as the path segment seperator
				.Replace(" ", "_") // Azure supports spaces but it leads to problems in URLs
				.Replace("#", "_") // Azure supports the # character but it leads to problems in URLs
				.Replace("'", "_") // Azure supports quotes but it leads to problems in URLs
				.TrimStart($"/devstoreaccount1")
				.TrimStart($"/{_blobContainer.Name}")
				.TrimStart("/");

			if (!allowEmptyName && string.IsNullOrWhiteSpace(blobName)) throw new ArgumentException("Name cannot be empty", nameof(blobName));
			if (blobName.Length > 1024) throw new ArgumentException("Name cannot be more than 1,024 characters long", nameof(blobName));

#if NET7_0_OR_GREATER
			// .NET 7 introduced allocation-free and highly optimized Regex APIs. Counting is especially easy and efficient.
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
			var segmentsCount = Regex.Count(input: blobName, pattern: "/");
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
#else
			var segmentsCount = 0;
			foreach (char c in blobName ?? string.Empty)
			{
				if (c == '/') segmentsCount++;
			}
#endif
			if (segmentsCount > 254) throw new ArgumentException("The number of path segments in a blob name cannot exceed 254.", nameof(blobName));

			return blobName;
		}

		#endregion
	}
}
