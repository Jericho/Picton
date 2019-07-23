using Azure.Core.Http;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using HeyRed.Mime;
using Picton.Interfaces;
using Picton.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="BlobClient"/> class.
	/// </summary>
	public static class BlobClientExtensions
	{
		private const int KB = 1024;
		private const int MB = KB * 1024;
		private const int GB = MB * 1024;
		private const long TB = GB * 1024L;

		private const long DEFAULT_PAGE_BLOB_SIZE = 5 * MB;

		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Attempt to acquire a lease asynchronously.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="leaseTime">The lease duration. If specified, this value must be between 15 and 60 seconds.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The lease Id.</returns>
		public static async Task<string> TryAcquireLeaseAsync(this BlobBaseClient blob, TimeSpan? leaseTime = null, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default)
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
				catch (StorageRequestFailedException e) when (e.ErrorCode == "LeaseAlreadyPresent")
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
		/// <param name="blob">The blob.</param>
		/// <param name="leaseTime">The lease duration. If specified, this value must be between 15 and 60 seconds.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The lease Id.</returns>
		public static async Task<string> AcquireLeaseAsync(this BlobBaseClient blob, TimeSpan? leaseTime = null, CancellationToken cancellationToken = default)
		{
			// From: https://msdn.microsoft.com/en-us/library/azure/ee691972.aspx
			// The Lease Blob operation establishes and manages a lock on a blob for write and delete operations.
			// The lock duration can be 15 to 60 seconds, or can be infinite.
			if (leaseTime.HasValue && (leaseTime.Value < TimeSpan.FromSeconds(15) | leaseTime.Value > TimeSpan.FromSeconds(60)))
			{
				throw new ArgumentOutOfRangeException(nameof(leaseTime), string.Format("{0} must be between 15 and 60 seconds", nameof(leaseTime)));
			}

			var leaseClient = blob.GetLeaseClient();
			var defaultLeaseTime = TimeSpan.FromSeconds(15);

			try
			{
				// Optimistically try to acquire the lease. The blob may not yet
				// exist. If it doesn't we handle the 404, create it, and retry below
				var response = await leaseClient.AcquireAsync(leaseTime.GetValueOrDefault(defaultLeaseTime).Seconds, null, cancellationToken).ConfigureAwait(false);
				return response.Value.LeaseId;
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(cancellationToken).ConfigureAwait(false);
				var response = await leaseClient.AcquireAsync(leaseTime.GetValueOrDefault(defaultLeaseTime).Seconds, null, cancellationToken).ConfigureAwait(false);

				return response.Value.LeaseId;
			}
		}

		/// <summary>
		/// Release a lease.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="leaseId">The lease Id.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task ReleaseLeaseAsync(this BlobBaseClient blob, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var leaseClient = new LeaseClient(blob, leaseId);
			return leaseClient.ReleaseAsync(null, cancellationToken);
		}

		/// <summary>
		/// Renews the lease asynchronously.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		/// <exception cref="ArgumentNullException">blob.</exception>
		public static Task RenewLeaseAsync(this BlobBaseClient blob, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var leaseClient = new LeaseClient(blob, leaseId);
			return leaseClient.RenewAsync(null, cancellationToken);
		}

		/// <summary>
		/// Attempts to renew a lease asynchronously.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="bool">boolean</see> value indicating if the lease was obtained.</returns>
		public static async Task<bool> TryRenewLeaseAsync(this BlobBaseClient blob, string leaseId, CancellationToken cancellationToken = default)
		{
			try
			{
				await RenewLeaseAsync(blob, leaseId, cancellationToken).ConfigureAwait(false);
				return true;
			}
			catch
			{
				return false;
			}
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task UploadStreamAsync(this PageBlobClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			PageBlobAccessConditions? pageBlobAccessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				pageBlobAccessConditions = new PageBlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			try
			{
				var pageRangesInfo = await blob.GetPageRangesAsync(null, null, pageBlobAccessConditions, cancellationToken).ConfigureAwait(false);
				await blob.ClearPagesAsync(new HttpRange(0, pageRangesInfo.Value.BlobContentLength), pageBlobAccessConditions, cancellationToken).ConfigureAwait(false);
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				await blob.UploadPagesAsync(new AlignedStream(content, PageBlobClient.PageBlobPageBytes), 0, null, pageBlobAccessConditions, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task UploadStreamAsync(this BlockBlobClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobAccessConditions? accessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessConditions = new BlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			BlobProperties properties = null;
			try
			{
				properties = await blob.GetPropertiesAsync(accessConditions, cancellationToken).ConfigureAwait(false);
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				var headers = new BlobHttpHeaders()
				{
					ContentType = properties?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				await blob.UploadAsync(content, headers, properties?.Metadata, accessConditions, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task UploadStreamAsync(this AppendBlobClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			AppendBlobAccessConditions? appendBlobAccessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				appendBlobAccessConditions = new AppendBlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			await blob.CreateIfNotExistsAsync(cancellationToken);
			await blob.AppendBlockAsync(content, null, appendBlobAccessConditions, null, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task UploadStreamAsync(this BlobClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobAccessConditions? accessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessConditions = new BlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			BlobProperties properties = null;
			try
			{
				properties = await blob.GetPropertiesAsync(accessConditions, cancellationToken).ConfigureAwait(false);
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				var headers = new BlobHttpHeaders()
				{
					ContentType = properties?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				await blob.UploadAsync(content, headers, properties?.Metadata, accessConditions, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task UploadStreamAsync(this BlobBaseClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob is PageBlobClient pageBlob) return pageBlob.UploadStreamAsync(content, leaseId, cancellationToken);
			else if (blob is BlockBlobClient blockBlob) return blockBlob.UploadStreamAsync(content, leaseId, cancellationToken);
			else if (blob is AppendBlobClient appendBlob) return appendBlob.UploadStreamAsync(content, leaseId, cancellationToken);
			else if (blob is BlobClient blobClient) return blobClient.UploadStreamAsync(content, leaseId, cancellationToken);
			else throw new Exception($"Unknow blob type: {blob.GetType().Name}");
		}

		/// <summary>
		/// Upload the content of a byte array to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The byte array.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task UploadBytesAsync(this BlobBaseClient blob, byte[] content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			var stream = new MemoryStream(content);
			return blob.UploadStreamAsync(stream, leaseId, cancellationToken);
		}

		/// <summary>
		/// Upload a string to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task UploadTextAsync(this BlobBaseClient blob, string content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			var buffer = content.ToBytes();
			return blob.UploadBytesAsync(buffer, leaseId, cancellationToken);
		}

		/// <summary>
		/// Append the content of a stream to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task AppendStreamAsync(this PageBlobClient blob, Stream content, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			PageBlobAccessConditions? pageBlobAccessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				pageBlobAccessConditions = new PageBlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			var offset = 0L;
			try
			{
				var pageRangesInfo = await blob.GetPageRangesAsync(null, null, pageBlobAccessConditions, cancellationToken).ConfigureAwait(false);
				offset = pageRangesInfo.Value.Body.PageRange.Max(r => r.End) + 1;
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				await blob.UploadPagesAsync(new AlignedStream(content, PageBlobClient.PageBlobPageBytes), offset, null, pageBlobAccessConditions, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Append the content of a stream to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task AppendStreamAsync(this BlockBlobClient blob, Stream content, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobAccessConditions? blobAccessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				blobAccessConditions = new BlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			BlobDownloadInfo downloadInfo = null;
			var combinedContent = new MultiStream();
			try
			{
				downloadInfo = await blob.DownloadAsync(accessConditions: blobAccessConditions, cancellationToken: cancellationToken).ConfigureAwait(false);
				combinedContent.AddStream(downloadInfo.Content);
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				var headers = new BlobHttpHeaders()
				{
					ContentType = downloadInfo?.Properties?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				combinedContent.AddStream(content);
				await blob.UploadAsync(combinedContent, headers, null, blobAccessConditions, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Append the content of a stream to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task AppendStreamAsync(this AppendBlobClient blob, Stream content, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			AppendBlobAccessConditions? appendBlobAccessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				appendBlobAccessConditions = new AppendBlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			await blob.CreateIfNotExistsAsync(cancellationToken);
			await blob.AppendBlockAsync(content, null, appendBlobAccessConditions, null, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Append the content of a stream to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task AppendStreamAsync(this BlobClient blob, Stream content, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobAccessConditions? blobAccessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				blobAccessConditions = new BlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			BlobDownloadInfo downloadInfo = null;
			var combinedContent = new MultiStream();
			try
			{
				downloadInfo = await blob.DownloadAsync(accessConditions: blobAccessConditions, cancellationToken: cancellationToken).ConfigureAwait(false);
				combinedContent.AddStream(downloadInfo.Content);
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				var headers = new BlobHttpHeaders()
				{
					ContentType = downloadInfo?.Properties?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				combinedContent.AddStream(content);
				await blob.UploadAsync(combinedContent, headers, null, blobAccessConditions, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Append the content of a stream to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task AppendStreamAsync(this BlobBaseClient blob, Stream content, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob is PageBlobClient pageBlob) return pageBlob.AppendStreamAsync(content, leaseId, cancellationToken);
			else if (blob is BlockBlobClient blockBlob) return blockBlob.AppendStreamAsync(content, leaseId, cancellationToken);
			else if (blob is AppendBlobClient appendBlob) return appendBlob.AppendStreamAsync(content, leaseId, cancellationToken);
			else if (blob is BlobClient blobClient) return blobClient.AppendStreamAsync(content, leaseId, cancellationToken);
			else throw new Exception($"Unknow blob type: {blob.GetType().Name}");
		}

		/// <summary>
		/// Append the content of a byte array to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="buffer">The byte array.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task AppendBytesAsync(this BlobBaseClient blob, byte[] buffer, string leaseId, CancellationToken cancellationToken = default)
		{
			var stream = new MemoryStream(buffer);
			return blob.AppendStreamAsync(stream, leaseId, cancellationToken);
		}

		/// <summary>
		/// Append a string to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task AppendTextAsync(this BlobBaseClient blob, string content, string leaseId, CancellationToken cancellationToken = default)
		{
			var buffer = content.ToBytes();
			return blob.AppendBytesAsync(buffer, leaseId, cancellationToken);
		}

		/// <summary>
		/// Sets user-defined metadata for the specified blob as one or more name-value pairs.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task SetMetadataAsync(this BlobBaseClient blob, IDictionary<string, string> metadata, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			BlobAccessConditions? accessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessConditions = new BlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
			}

			return blob.SetMetadataAsync(metadata, accessConditions, cancellationToken);
		}

		/// <summary>
		/// Download the content of a blob as a string.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The content as a string.</returns>
		public static async Task<string> DownloadTextAsync(this BlobBaseClient blob, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			using (var stream = new MemoryStream())
			{
				var response = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
				using (var reader = new StreamReader(response.Value.Content, true))
				{
					stream.Position = 0;
					return await reader.ReadToEndAsync().ConfigureAwait(false);
				}
			}
		}

		/// <summary>
		/// Download the content of a blob as a byte array.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The content as a byte array.</returns>
		public static async Task<byte[]> DownloadByteArrayAsync(this BlobBaseClient blob, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var response = await blob.DownloadAsync(cancellationToken).ConfigureAwait(false);
			var stream = response.Value.Content;

			if (stream is MemoryStream ms)
			{
				return ms.ToArray();
			}
			else
			{
				using (var memoryStream = new MemoryStream())
				{
					await response.Value.Content.CopyToAsync(memoryStream).ConfigureAwait(false);
					return memoryStream.ToArray();
				}
			}
		}

		/// <summary>
		/// Asynchronously copy a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="destinationBlobName">The name of the blob where the source wil be copied.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		//public static async Task CopyAsync(this BlobBaseClient blob, string destinationBlobName, string leaseId = null, bool waitForCompletion = true, CancellationToken cancellationToken = default)
		//{
		//	if (blob == null) throw new ArgumentNullException(nameof(blob));
		//	if (string.IsNullOrEmpty(destinationBlobName)) throw new ArgumentNullException(nameof(destinationBlobName));

		//	BlobAccessConditions? accessConditions = null;
		//	if (!string.IsNullOrEmpty(leaseId))
		//	{
		//		accessConditions = new BlobAccessConditions { LeaseAccessConditions = new LeaseAccessConditions { LeaseId = leaseId } };
		//	}

		//	var destinationUri = new Uri(blob.Uri, destinationBlobName);

		//	var destinationBlob = new BlobClient(destinationUri);
		//	var copyOperation = await destinationBlob.StartCopyFromUriAsync(blob.Uri, null, accessConditions, null, cancellationToken).ConfigureAwait(false);

		//	if (waitForCompletion)
		//	{
		//		await copyOperation.WaitCompletionAsync(cancellationToken).ConfigureAwait(false);
		//	}
		//}

		/// <summary>
		/// Creates a Shared Access Signature URI for the blob.
		/// </summary>
		/// <param name="blob">The blob to be shared.</param>
		/// <param name="permissions">The permissions granted to the shared access signature.</param>
		/// <param name="duration">The period of time the shared access signature is valid for. If this parameter is omited, it defaults to 15 minutes.</param>
		/// <param name="systemClock">Allows dependency injection for unit tesing puposes. Feel free to ignore this parameter.</param>
		/// <returns>The URI.</returns>
		/// <remarks>
		/// Inspired by http://gauravmantri.com/2013/02/13/revisiting-windows-azure-shared-access-signature/ .
		/// </remarks>
		public static async Task<string> GetSharedAccessSignatureUriAsync(this BlobBaseClient blob, StorageSharedKeyCredential sharedKeyCredential, BlobSasPermissions permissions, TimeSpan? duration = null, ISystemClock systemClock = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var now = (systemClock ?? SystemClock.Instance).UtcNow;
			var properties = await blob.GetPropertiesAsync(null, cancellationToken).ConfigureAwait(false);

			var blobSasBuilder = new BlobSasBuilder
			{
				StartTime = now.AddMinutes(-5), // Start time is back by 5 minutes to take clock skewness into consideration
				ExpiryTime = now.Add(duration.GetValueOrDefault(TimeSpan.FromMinutes(15))),
				Permissions = string.Format(
					"{0}{1}{2}{3}{4}",
					permissions.Add ? "a" : string.Empty,
					permissions.Create ? "c" : string.Empty,
					permissions.Delete ? "d" : string.Empty,
					permissions.Read ? "r" : string.Empty,
					permissions.Write ? "w" : string.Empty)
			};

			var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(sharedKeyCredential).ToString();

			return string.Format(CultureInfo.InvariantCulture, "{0}{1}", blob.Uri, sasQueryParameters);
		}

		/// <summary>
		/// Indicates if a blob exists.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the blob exists; false otherwise.</returns>
		public static async Task<bool> ExistsAsync(this BlobBaseClient blob, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			try
			{
				await blob.GetPropertiesAsync(null, cancellationToken).ConfigureAwait(false);
				return true;
			}
			catch (StorageRequestFailedException e) when (e.ErrorCode == "BlobNotFound" || e.ErrorCode == "ResourceNotFound")
			{
				return false;
			}
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task CreateAsync(this PageBlobClient blob, CancellationToken cancellationToken = default)
		{
			return blob.CreateAsync(DEFAULT_PAGE_BLOB_SIZE, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="blobSize">The blob size.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task CreateAsync(this PageBlobClient blob, long blobSize, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			return blob.CreateAsync(blobSize, null, headers, null, null, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task CreateAsync(this BlockBlobClient blob, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			var content = new MemoryStream();
			return blob.UploadAsync(content, headers, null, null, null, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task CreateAsync(this AppendBlobClient blob, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			return blob.CreateAsync(headers, null, null, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task CreateAsync(this BlobClient blob, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			var content = new MemoryStream();
			return blob.UploadAsync(content, headers, null, null, null, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task CreateAsync(this BlobBaseClient blob, CancellationToken cancellationToken = default)
		{
			if (blob is PageBlobClient pageBlob) return pageBlob.CreateAsync(cancellationToken);
			else if (blob is BlockBlobClient blockBlob) return blockBlob.CreateAsync(cancellationToken);
			else if (blob is AppendBlobClient appendBlob) return appendBlob.CreateAsync(cancellationToken);
			else if (blob is BlobClient blobClient) return blobClient.CreateAsync(cancellationToken);
			else throw new Exception($"Unknow blob type: {blob.GetType().Name}");
		}

		/// <summary>
		/// Creates a blob if it doesn't already exists.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the blob was created; false otherwise.</returns>
		public static async Task<bool> CreateIfNotExistsAsync(this PageBlobClient blob, long blobSize, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var exists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
			if (exists) return false;

			await blob.CreateAsync(blobSize, cancellationToken).ConfigureAwait(false);
			return true;
		}

		/// <summary>
		/// Creates a blob if it doesn't already exists.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the blob was created; false otherwise.</returns>
		public static async Task<bool> CreateIfNotExistsAsync(this BlobBaseClient blob, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var exists = await blob.ExistsAsync(cancellationToken).ConfigureAwait(false);
			if (exists) return false;

			await blob.CreateAsync(cancellationToken).ConfigureAwait(false);
			return true;
		}

		#endregion
	}
}
