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
				throw new ArgumentOutOfRangeException(nameof(maxLeaseAttempts), "The number of attempts must be between 1 and 10");
			}

			var leaseId = (string)null;
			for (var attempts = 0; attempts < maxLeaseAttempts; attempts++)
			{
				try
				{
					leaseId = await blob.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);
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
				await blob.CreateAsync(null, true, cancellationToken).ConfigureAwait(false);
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
		public static Task ReleaseLeaseAsync(this BlobBaseClient blob, string leaseId, CancellationToken cancellationToken = default)
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
		public static Task RenewLeaseAsync(this BlobBaseClient blob, string leaseId, CancellationToken cancellationToken = default)
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
		public static async Task UploadStreamAsync(this PageBlobClient blob, Stream content, string mimeType = null, string cacheControl = null, string contentEncoding = null, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			PageBlobRequestConditions pageBlobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				pageBlobRequestConditions = new PageBlobRequestConditions { LeaseId = leaseId };
			}

			try
			{
				var pageRangesInfo = await blob.GetPageRangesAsync(null, null, pageBlobRequestConditions, cancellationToken).ConfigureAwait(false);
				await blob.ClearPagesAsync(new HttpRange(0, pageRangesInfo.Value.BlobContentLength), pageBlobRequestConditions, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(null, true, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				await blob.UploadPagesAsync(new AlignedStream(content, blob.PageBlobPageBytes), 0, null, pageBlobRequestConditions, null, cancellationToken).ConfigureAwait(false);
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
		public static async Task UploadStreamAsync(this BlockBlobClient blob, Stream content, string mimeType = null, string cacheControl = null, string contentEncoding = null, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobRequestConditions accessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			BlobProperties properties = null;
			try
			{
				properties = await blob.GetPropertiesAsync(accessConditions, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(null, true, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				var headers = new BlobHttpHeaders()
				{
					ContentType = properties?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				await blob.UploadAsync(content, headers, properties?.Metadata, accessConditions, null, null, cancellationToken).ConfigureAwait(false);
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
		public static async Task UploadStreamAsync(this AppendBlobClient blob, Stream content, string mimeType = null, string cacheControl = null, string contentEncoding = null, string leaseId = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			AppendBlobRequestConditions appendRequestConditions = null;
			BlobRequestConditions accessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				appendRequestConditions = new AppendBlobRequestConditions { LeaseId = leaseId };
				accessConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			try
			{
				await blob.GetPropertiesAsync(accessConditions, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(null, true, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				await blob.AppendBlockAsync(content, null, appendRequestConditions, null, cancellationToken).ConfigureAwait(false);
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

			BlobRequestConditions accessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			BlobProperties properties = null;
			try
			{
				properties = await blob.GetPropertiesAsync(accessConditions, cancellationToken).ConfigureAwait(false);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(null, true, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				var headers = new BlobHttpHeaders()
				{
					ContentType = mimeType ?? properties?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath)),
					CacheControl = cacheControl ?? properties?.CacheControl,
					ContentEncoding = contentEncoding ?? properties?.ContentEncoding
				};

				await blob.UploadAsync(content, headers, properties?.Metadata, accessConditions, null, null, default, cancellationToken).ConfigureAwait(false);
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
		public static Task UploadStreamAsync(this BlobBaseClient blob, Stream content, string mimeType = null, string cacheControl = null, string contentEncoding = null, string leaseId = null, CancellationToken cancellationToken = default)
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
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task UploadBytesAsync(this BlobBaseClient blob, byte[] content, string mimeType = null, string cacheControl = null, string contentEncoding = null, string leaseId = null, CancellationToken cancellationToken = default)
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
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static Task UploadTextAsync(this BlobBaseClient blob, string content, string mimeType = null, string cacheControl = null, string contentEncoding = null, string leaseId = null, CancellationToken cancellationToken = default)
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
		public static async Task AppendStreamAsync(this PageBlobClient blob, Stream content, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			PageBlobRequestConditions pageBlobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				pageBlobRequestConditions = new PageBlobRequestConditions { LeaseId = leaseId };
			}

			var offset = 0L;
			try
			{
				var pageRangesInfo = await blob.GetPageRangesAsync(null, null, pageBlobRequestConditions, cancellationToken).ConfigureAwait(false);
				offset = pageRangesInfo.Value.PageRanges.Sum(r => r.Length ?? 0);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(null, true, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				await blob.UploadPagesAsync(new AlignedStream(content, blob.PageBlobPageBytes), offset, null, pageBlobRequestConditions, null, cancellationToken).ConfigureAwait(false);
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

			BlobRequestConditions blobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				blobRequestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			BlobDownloadInfo downloadInfo = null;
			var combinedContent = new MultiStream();
			try
			{
				downloadInfo = await blob.DownloadAsync(default, blobRequestConditions, false, cancellationToken).ConfigureAwait(false);

				// The content in downloadInfo is non-seekable. Therfeore, must make a copy
				var memStream = new MemoryStream();
				downloadInfo.Content.CopyTo(memStream);

				combinedContent.AddStream(memStream);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(null, true, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				var headers = new BlobHttpHeaders()
				{
					ContentType = downloadInfo?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				combinedContent.AddStream(content);
				await blob.UploadAsync(combinedContent, headers, null, blobRequestConditions, null, null, cancellationToken).ConfigureAwait(false);
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

			AppendBlobRequestConditions appendBlobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				appendBlobRequestConditions = new AppendBlobRequestConditions { LeaseId = leaseId };
			}

			await blob.CreateIfNotExistsAsync(null, null, cancellationToken).ConfigureAwait(false);
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
		public static async Task AppendStreamAsync(this BlobClient blob, Stream content, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));
			if (content == null) throw new ArgumentNullException(nameof(content));

			content.Position = 0; // Rewind the stream. IMPORTANT!

			BlobRequestConditions blobRequestConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				blobRequestConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			BlobDownloadInfo downloadInfo = null;
			var combinedContent = new MultiStream();
			try
			{
				downloadInfo = await blob.DownloadAsync(conditions: blobRequestConditions, cancellationToken: cancellationToken).ConfigureAwait(false);

				// The content in downloadInfo is non-seekable. Therfeore, must make a copy
				var memStream = new MemoryStream();
				downloadInfo.Content.CopyTo(memStream);

				combinedContent.AddStream(memStream);
			}
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound")
			{
				await blob.CreateAsync(null, true, cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				var headers = new BlobHttpHeaders()
				{
					ContentType = downloadInfo?.ContentType ?? MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
				};

				combinedContent.AddStream(content);
				await blob.UploadAsync(content: combinedContent, httpHeaders: headers, conditions: blobRequestConditions, cancellationToken: cancellationToken).ConfigureAwait(false);
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

			BlobRequestConditions accessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessConditions = new BlobRequestConditions { LeaseId = leaseId };
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
			catch (RequestFailedException e) when (e.ErrorCode == "BlobNotFound" || e.ErrorCode == "ResourceNotFound")
			{
				return false;
			}
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task<Response<BlobContentInfo>> CreateAsync(this PageBlobClient blob, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, CancellationToken cancellationToken = default)
		{
			return blob.CreateAsync(DEFAULT_PAGE_BLOB_SIZE, metadata, overwriteIfExists, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="blobSize">The blob size.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task<Response<BlobContentInfo>> CreateAsync(this PageBlobClient blob, long blobSize, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			PageBlobRequestConditions accessConditions = null;
			if (!overwriteIfExists)
			{
				accessConditions = new PageBlobRequestConditions
				{
					IfNoneMatch = new ETag("*")
				};
			}

			return blob.CreateAsync(blobSize, null, headers, metadata, accessConditions, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task<Response<BlobContentInfo>> CreateAsync(this BlockBlobClient blob, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			BlobRequestConditions accessConditions = null;
			if (!overwriteIfExists)
			{
				accessConditions = new BlobRequestConditions
				{
					IfNoneMatch = new ETag("*")
				};
			}

			var content = new MemoryStream();
			return blob.UploadAsync(content, headers, metadata, accessConditions, null, null, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task<Response<BlobContentInfo>> CreateAsync(this AppendBlobClient blob, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			AppendBlobRequestConditions accessConditions = null;
			if (!overwriteIfExists)
			{
				accessConditions = new AppendBlobRequestConditions
				{
					IfNoneMatch = new ETag("*")
				};
			}

			return blob.CreateAsync(headers, metadata, accessConditions, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task<Response<BlobContentInfo>> CreateAsync(this BlobClient blob, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			var headers = new BlobHttpHeaders()
			{
				ContentType = MimeTypesMap.GetMimeType(Path.GetExtension(blob.Uri.LocalPath))
			};

			BlobRequestConditions accessConditions = null;
			if (!overwriteIfExists)
			{
				accessConditions = new BlobRequestConditions
				{
					IfNoneMatch = new ETag("*")
				};
			}

			var content = new MemoryStream();
			return blob.UploadAsync(content, headers, null, accessConditions, null, null, default, cancellationToken);
		}

		/// <summary>
		/// Creates a blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="metadata">Custom metadata to set for this blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		public static Task CreateAsync(this BlobBaseClient blob, IDictionary<string, string> metadata = null, bool overwriteIfExists = true, CancellationToken cancellationToken = default)
		{
			if (blob is PageBlobClient pageBlob) return pageBlob.CreateAsync(metadata, overwriteIfExists, cancellationToken);
			else if (blob is BlockBlobClient blockBlob) return blockBlob.CreateAsync(metadata, overwriteIfExists, cancellationToken);
			else if (blob is AppendBlobClient appendBlob) return appendBlob.CreateAsync(metadata, overwriteIfExists, cancellationToken);
			else if (blob is BlobClient blobClient) return blobClient.CreateAsync(metadata, overwriteIfExists, cancellationToken);
			else throw new Exception($"Unknow blob type: {blob.GetType().Name}");
		}

		/// <summary>
		/// Gets all user-defined metadata, standard HTTP properties, and system properties for the blob. It does not return the content of the blob.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="leaseId">The lease identifier.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>A <see cref="Task"/> object that represents the asynchronous operation.</returns>
		public static async Task<BlobProperties> GetProperties(this BlobBaseClient blob, string leaseId, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			BlobRequestConditions accessConditions = null;
			if (!string.IsNullOrEmpty(leaseId))
			{
				accessConditions = new BlobRequestConditions { LeaseId = leaseId };
			}

			var response = await blob.GetPropertiesAsync(accessConditions, cancellationToken).ConfigureAwait(false);
			return response.Value;
		}

		/// <summary>
		/// Creates a blob if it doesn't already exists.
		/// </summary>
		/// <param name="blob">The blob.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the blob was created; false otherwise.</returns>
		public static async Task<bool> CreateIfNotExistsAsync(this BlobClient blob, IDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
		{
			if (blob == null) throw new ArgumentNullException(nameof(blob));

			try
			{
				await blob.CreateAsync(metadata, false, cancellationToken).ConfigureAwait(false);
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
