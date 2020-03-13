using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	public interface IBlobManager
	{
		Task AppendBytesAsync(string blobName, byte[] content, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task AppendStreamAsync(string blobName, Stream stream, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task AppendTextAsync(string blobName, string content, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task CopyBlobAsync(string sourceBlobName, string destinationBlobName, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default);

		Task DeleteBlobsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default);

		Task<byte[]> GetBlobContentAsync(string blobName, CancellationToken cancellationToken = default);

		Task<BlobDownloadInfo> GetBlobContentAsync(string blobName, Stream outputStream, CancellationToken cancellationToken = default);

		BlobClient GetBlobReference(string blobName);

		Pageable<BlobItem> ListBlobs(string prefix, bool includeMetadata = false, CancellationToken cancellationToken = default);

		Task MoveBlobAsync(string sourceBlobName, string destinationBlobName, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task UploadBytesAsync(string blobName, byte[] buffer, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task UploadFileAsync(string blobName, string fileName, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task UploadStreamAsync(string blobName, Stream stream, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task UploadTextAsync(string blobName, string content, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);
	}
}
