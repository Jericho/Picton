using Azure;
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

		private const long DEFAULT_PAGE_BLOB_SIZE = 5 * MB;

		private static readonly Stream EmptyStream = new MemoryStream();
		private static readonly IDictionary<string, string> EmptyDictionary = new Dictionary<string, string>();

		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Attempt to acquire a lease asynchronously.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="leaseTime">The lease duration. If specified, this value must be between 15 and 60 seconds.</param>
		/// <param name="maxAttempts">The maximum number of attempts.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The lease Id.</returns>
		public static async Task<string> TryAcquireLeaseAsync(this BlobBaseClient blob, TimeSpan? leaseTime = null, int maxAttempts = 1, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (maxAttempts < 1 || maxAttempts > 10)
			{
				throw new ArgumentOutOfRangeException(nameof(maxAttempts), "The number of attempts must be between 1 and 10");
			}

			var leaseId = (string)null;
			for (var attempts = 0; attempts < maxAttempts; attempts++)
			{
				try
				{
					leaseId = await blob.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);
					if (!string.IsNullOrEmpty(leaseId)) break;
				}
				catch (RequestFailedException e) when (e.ErrorCode == "LeaseAlreadyPresent")
				{
					if (attempts < maxAttempts - 1)
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
				throw new ArgumentOutOfRangeException(nameof(leaseTime), "Lease duration must be between 15 and 60 seconds");
			}

			var leaseClient = new BlobLeaseClient(blob, null);
			var defaultLeaseTime = TimeSpan.FromSeconds(15);

			try
			{
				// Optimistically try to acquire the lease. The blob may not yet
				// exist. If it doesn't we handle the 404, create it, and retry below
				var response = await leaseClient.AcquireAsync(leaseTime.GetValueOrDefault(defaultLeaseTime), null, cancellationToken).ConfigureAwait(false);
				return response.Value.LeaseId;
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(null, null, true, cancellationToken: cancellationToken).ConfigureAwait(false);
				var response = await leaseClient.AcquireAsync(leaseTime.GetValueOrDefault(defaultLeaseTime), null, cancellationToken).ConfigureAwait(false);

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
		public static Task ReleaseLeaseAsync(this BlobBaseClient blob, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var leaseClient = new BlobLeaseClient(blob, leaseId);
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
		public static Task RenewLeaseAsync(this BlobBaseClient blob, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var leaseClient = new BlobLeaseClient(blob, leaseId);
			return leaseClient.RenewAsync(null, cancellationToken);
		}

		/// <summary>
		/// Attempts to renew a lease asynchronously.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="bool">boolean</see> value indicating if the lease was obtained.</returns>
		public static async Task<bool> TryRenewLeaseAsync(this BlobBaseClient blob, string leaseId = null, CancellationToken cancellationToken = default)
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
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task UploadStreamAsync(this PageBlobClient blob, Stream content, string leaseId = null, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			PageBlobRequestConditions pageBlobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				pageBlobRequestConditions = new PageBlobRequestConditions { LeaseId = leaseId };
			}

			var alignedContent = new AlignedStream(content, blob.PageBlobPageBytes);

			try
			{
				var pageRangesInfo = await blob.GetPageRangesAsync(null, null, pageBlobRequestConditions, cancellationToken).ConfigureAwait(false);
				await blob.ClearPagesAsync(new HttpRange(0, pageRangesInfo.Value.BlobContentLength), pageBlobRequestConditions, cancellationToken).ConfigureAwait(false);
				await blob.UploadPagesAsync(alignedContent, 0, null, pageBlobRequestConditions, null, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(alignedContent, BlobClientExtensions.EmptyDictionary, true, mimeType, cacheControl, contentEncoding, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task UploadStreamAsync(this BlockBlobClient blob, Stream content, string leaseId = null, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobRequestConditions requestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				requestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			try
			{
				BlobProperties properties = await blob.GetPropertiesAsync(requestConditions, cancellationToken).ConfigureAwait(false);

				var headers = new BlobHttpHeaders()
				{
					ContentType = properties?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				await blob.UploadAsync(content ?? BlobClientExtensions.EmptyStream, headers, properties?.Metadata ?? BlobClientExtensions.EmptyDictionary, requestConditions, null, null, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(content, BlobClientExtensions.EmptyDictionary, true, mimeType, cacheControl, contentEncoding, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task UploadStreamAsync(this AppendBlobClient blob, Stream content, string leaseId = null, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			AppendBlobRequestConditions requestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				requestConditions = new AppendBlobRequestConditions { LeaseId = leaseId };
			}

			try
			{
				await blob.AppendBlockAsync(content, null, requestConditions, null, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(content, BlobClientExtensions.EmptyDictionary, true, mimeType, cacheControl, contentEncoding, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="mimeType">The MIME type.</param>
		/// <param name="cacheControl">The directives for caching mechanisms.</param>
		/// <param name="contentEncoding">The content encoding.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task UploadStreamAsync(this BlobClient blob, Stream content, string mimeType = null, string cacheControl = null, string contentEncoding = null, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobRequestConditions requestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				requestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			try
			{
				BlobProperties properties = await blob.GetPropertiesAsync(requestConditions, cancellationToken).ConfigureAwait(false);

				var headers = new BlobHttpHeaders()
				{
					ContentType = mimeType ?? properties?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath)),
					CacheControl = cacheControl ?? properties?.CacheControl,
					ContentEncoding = contentEncoding ?? properties?.ContentEncoding
				};

				await blob.UploadAsync(content ?? BlobClientExtensions.EmptyStream, headers, properties?.Metadata, requestConditions, null, null, default, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(content, BlobClientExtensions.EmptyDictionary, true, mimeType, cacheControl, contentEncoding, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Upload the content of a stream to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task UploadStreamAsync(this BlobBaseClient blob, Stream content, string leaseId = null, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			if (blob is PageBlobClient pageBlob) return pageBlob.UploadStreamAsync(content, mimeType, cacheControl, contentEncoding, leaseId, cancellationToken);
			else if (blob is BlockBlobClient blockBlob) return blockBlob.UploadStreamAsync(content, mimeType, cacheControl, contentEncoding, leaseId, cancellationToken);
			else if (blob is AppendBlobClient appendBlob) return appendBlob.UploadStreamAsync(content, mimeType, cacheControl, contentEncoding, leaseId, cancellationToken);
			else if (blob is BlobClient blobClient) return blobClient.UploadStreamAsync(content, mimeType, cacheControl, contentEncoding, leaseId, cancellationToken);
			else throw new Exception($"Unknow blob type: {blob.GetType().Name}");
		}

		/// <summary>
		/// Upload the content of a byte array to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The byte array.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task UploadBytesAsync(this BlobBaseClient blob, byte[] content, string leaseId = null, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			var stream = new MemoryStream(content);
			return blob.UploadStreamAsync(stream, mimeType, cacheControl, contentEncoding, leaseId, cancellationToken);
		}

		/// <summary>
		/// Upload a string to a blob. If the blog already exist, it will be overwritten.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task UploadTextAsync(this BlobBaseClient blob, string content, string leaseId = null, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			var buffer = content.ToBytes();
			return blob.UploadBytesAsync(buffer, mimeType, cacheControl, contentEncoding, leaseId, cancellationToken);
		}

		/// <summary>
		/// Append the content of a stream to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task AppendStreamAsync(this PageBlobClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			PageBlobRequestConditions pageBlobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				pageBlobRequestConditions = new PageBlobRequestConditions { LeaseId = leaseId };
			}

			var alignedContent = new AlignedStream(content, blob.PageBlobPageBytes);

			var offset = 0L;
			try
			{
				var pageRangesInfo = await blob.GetPageRangesAsync(null, null, pageBlobRequestConditions, cancellationToken).ConfigureAwait(false);
				offset = pageRangesInfo.Value.PageRanges.Sum(r => r.Length ?? 0);
				await blob.UploadPagesAsync(alignedContent, offset, null, pageBlobRequestConditions, null, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(alignedContent, BlobClientExtensions.EmptyDictionary, true, cancellationToken: cancellationToken).ConfigureAwait(false);
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
		public static async Task AppendStreamAsync(this BlockBlobClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobRequestConditions blobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				blobRequestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			try
			{
				BlobDownloadInfo downloadInfo = await blob.DownloadAsync(default, blobRequestConditions, false, cancellationToken).ConfigureAwait(false);

				// The content in downloadInfo is non-seekable. Therfeore, must make a copy
				var memStream = new MemoryStream();
				downloadInfo.Content.CopyTo(memStream);

				// Combine the existing and the new content
				var combinedContent = new MultiStream();
				combinedContent.AddStream(memStream);
				combinedContent.AddStream(content);

				var headers = new BlobHttpHeaders()
				{
					ContentType = downloadInfo?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				await blob.UploadAsync(combinedContent ?? BlobClientExtensions.EmptyStream, headers, BlobClientExtensions.EmptyDictionary, blobRequestConditions, null, null, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(content, BlobClientExtensions.EmptyDictionary, true, cancellationToken: cancellationToken).ConfigureAwait(false);
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
		public static async Task AppendStreamAsync(this AppendBlobClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			AppendBlobRequestConditions appendBlobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				appendBlobRequestConditions = new AppendBlobRequestConditions { LeaseId = leaseId };
			}

			await blob.CreateIfNotExistsAsync(headers, BlobClientExtensions.EmptyDictionary, cancellationToken).ConfigureAwait(false);
			await blob.AppendBlockAsync(content, null, appendBlobRequestConditions, null, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Append the content of a stream to a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The stream.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task AppendStreamAsync(this BlobClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobRequestConditions blobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				blobRequestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			var combinedContent = new MultiStream();

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			try
			{
				BlobDownloadInfo downloadInfo = await blob.DownloadAsync(conditions: blobRequestConditions, cancellationToken: cancellationToken).ConfigureAwait(false);

				headers.ContentType = downloadInfo?.ContentType ?? headers.ContentType;

				// The content in downloadInfo is non-seekable. Therfore, must make a copy
				var memStream = new MemoryStream();
				downloadInfo.Content.CopyTo(memStream);

				// Combine the existing and the new content
				combinedContent.AddStream(memStream);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				// The blob does not exist. We will therefore create a new one.
			}
			finally
			{
				// Combine the existing and the new content
				combinedContent.AddStream(content);

				// Overwrite the content of the existing blob (or create a new blob) with the combined content
				await blob.UploadAsync(combinedContent ?? BlobClientExtensions.EmptyStream, headers, BlobClientExtensions.EmptyDictionary, blobRequestConditions, null, null, default, cancellationToken).ConfigureAwait(false);
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
		public static Task AppendStreamAsync(this BlobBaseClient blob, Stream content, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

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
		public static Task AppendBytesAsync(this BlobBaseClient blob, byte[] buffer, string leaseId = null, CancellationToken cancellationToken = default)
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
		public static Task AppendTextAsync(this BlobBaseClient blob, string content, string leaseId = null, CancellationToken cancellationToken = default)
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
		public static Task SetMetadataAsync(this BlobBaseClient blob, IDictionary<string, string> metadata, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			BlobRequestConditions requestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				requestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			return blob.SetMetadataAsync(metadata, requestConditions, cancellationToken);
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
		/// Creates a Shared Access Signature URI for the blob.
		/// </summary>
		/// <param name="blob">The blob to be shared.</param>
		/// <param name="sharedKeyCredential">The storage account's shared key credential.</param>
		/// <param name="permissions">The permissions granted to the shared access signature.</param>
		/// <param name="duration">The period of time the shared access signature is valid for. If this parameter is omited, it defaults to 15 minutes.</param>
		/// <param name="systemClock">Allows dependency injection for unit tesing puposes. Feel free to ignore this parameter.</param>
		/// <returns>The URI.</returns>
		/// <remarks>
		/// Inspired by http://gauravmantri.com/2013/02/13/revisiting-windows-azure-shared-access-signature/ .
		/// </remarks>
		public static string GetSharedAccessSignatureUri(this BlobBaseClient blob, StorageSharedKeyCredential sharedKeyCredential, BlobSasPermissions permissions, TimeSpan? duration = null, ISystemClock systemClock = null)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var now = (systemClock ?? SystemClock.Instance).UtcNow;

			var blobSasBuilder = new BlobSasBuilder
			{
				StartsOn = now.AddMinutes(-5), // Start time is back by 5 minutes to take clock skewness into consideration
				ExpiresOn = now.Add(duration.GetValueOrDefault(TimeSpan.FromMinutes(15)))
			};
			blobSasBuilder.SetPermissions(permissions);

			var sasQueryParameters = blobSasBuilder.ToSasQueryParameters(sharedKeyCredential).ToString();

			return string.Format(CultureInfo.InvariantCulture, "{0}?{1}", blob.Uri, sasQueryParameters);
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
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound" || e.ErrorCode == "ResourceNotFound")
			{
				return false;
			}
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="overwriteIfExists">Indicates if existing blob should be overwritten.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Azure.Response{T}">response</see> describing the state of the blob.</returns>
		public static Task CreateAsync(this PageBlobClient blob, Stream content = null, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			return blob.CreateAsync(DEFAULT_PAGE_BLOB_SIZE, content, metadata, overwriteIfExists, mimeType, cacheControl, contentEncoding, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="blobSize">The maximum size for the page blob, up to 8 TB. The size must be aligned to a 512-byte boundary.</param>
		/// <param name="content">The content.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="overwriteIfExists">Indicates if existing blob should be overwritten.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Azure.Response{T}">response</see> describing the state of the blob.</returns>
		public static async Task CreateAsync(this PageBlobClient blob, long blobSize, Stream content = null, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				CacheControl = cacheControl,
				ContentEncoding = contentEncoding,
				ContentType = mimeType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			PageBlobRequestConditions requestConditions = null;
			if (!overwriteIfExists)
			{
				requestConditions = new PageBlobRequestConditions
				{
					IfNoneMatch = ETag.All
				};
			}

			await blob.CreateAsync(blobSize, null, headers, metadata, requestConditions, cancellationToken).ConfigureAwait(false);

			if (content != null)
			{
				await blob.UploadPagesAsync(content, 0, null, requestConditions, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="overwriteIfExists">Indicates if existing blob should be overwritten.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Azure.Response{T}">response</see> describing the state of the blob.</returns>
		public static async Task CreateAsync(this BlockBlobClient blob, Stream content = null, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				CacheControl = cacheControl,
				ContentEncoding = contentEncoding,
				ContentType = mimeType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			BlobRequestConditions requestConditions = null;
			if (!overwriteIfExists)
			{
				requestConditions = new BlobRequestConditions
				{
					IfNoneMatch = ETag.All
				};
			}

			await blob.UploadAsync(content ?? BlobClientExtensions.EmptyStream, headers, metadata, requestConditions, null, null, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="overwriteIfExists">Indicates if existing blob should be overwritten.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Azure.Response{T}">response</see> describing the state of the blob.</returns>
		public static async Task CreateAsync(this AppendBlobClient blob, Stream content = null, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				CacheControl = cacheControl,
				ContentEncoding = contentEncoding,
				ContentType = mimeType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			AppendBlobRequestConditions requestConditions = null;
			if (!overwriteIfExists)
			{
				requestConditions = new AppendBlobRequestConditions
				{
					IfNoneMatch = ETag.All
				};
			}

			await blob.CreateAsync(headers, metadata, requestConditions, cancellationToken).ConfigureAwait(false);

			if (content != null)
			{
				await blob.AppendBlockAsync(content, null, requestConditions, null, cancellationToken).ConfigureAwait(false);
			}
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="overwriteIfExists">Indicates if existing blob should be overwritten.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Azure.Response{T}">response</see> describing the state of the blob.</returns>
		public static async Task CreateAsync(this BlobClient blob, Stream content = null, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				CacheControl = cacheControl,
				ContentEncoding = contentEncoding,
				ContentType = mimeType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			BlobRequestConditions requestConditions = null;
			if (!overwriteIfExists)
			{
				requestConditions = new BlobRequestConditions
				{
					IfNoneMatch = ETag.All
				};
			}

			await blob.UploadAsync(content ?? BlobClientExtensions.EmptyStream, headers, metadata, requestConditions, null, null, default, cancellationToken).ConfigureAwait(false);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="overwriteIfExists">Indicates if existing blob should be overwritten.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Azure.Response{T}">response</see> describing the state of the blob.</returns>
		public static Task CreateAsync(this BlobBaseClient blob, Stream content = null, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob is PageBlobClient pageBlob) return pageBlob.CreateAsync(content, metadata, overwriteIfExists, mimeType, cacheControl, contentEncoding, cancellationToken);
			else if (blob is BlockBlobClient blockBlob) return blockBlob.CreateAsync(content, metadata, overwriteIfExists, mimeType, cacheControl, contentEncoding, cancellationToken);
			else if (blob is AppendBlobClient appendBlob) return appendBlob.CreateAsync(content, metadata, overwriteIfExists, mimeType, cacheControl, contentEncoding, cancellationToken);
			else if (blob is BlobClient blobClient) return blobClient.CreateAsync(content, metadata, overwriteIfExists, mimeType, cacheControl, contentEncoding, cancellationToken);
			else throw new Exception($"Unknow blob type: {blob.GetType().Name}");
		}

		/// <summary>
		/// Gets all user-defined metadata, standard HTTP properties, and system properties for the blob. It does not return the content of the blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task<BlobProperties> GetProperties(this BlobBaseClient blob, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			BlobRequestConditions requestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				requestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			BlobProperties properties = await blob.GetPropertiesAsync(requestConditions, cancellationToken).ConfigureAwait(false);
			return properties;
		}

		/// <summary>
		/// Creates a blob if it doesn't already exists.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="content">The content.</param>
		/// <param name="metadata">The metadata.</param>
		/// <param name="mimeType">The MIME content type of the blob.</param>
		/// <param name="cacheControl">Specify directives for caching mechanisms.</param>
		/// <param name="contentEncoding">Specifies which content encoding has been applied to the blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the blob was created; false otherwise.</returns>
		public static async Task<bool> CreateIfNotExistsAsync(this BlobClient blob, Stream content = null, IDictionary<string, string> metadata = null, string mimeType = null, string cacheControl = null, string contentEncoding = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			try
			{
				await blob.CreateAsync(content, metadata, false, mimeType, cacheControl, contentEncoding, cancellationToken).ConfigureAwait(false);
				return true; // True indicates that blob was created
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobAlreadyExists")
			{
				return false; // False indicates that blob was not created
			}
		}

		#endregion
	}
}
