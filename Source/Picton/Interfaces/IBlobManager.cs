using Microsoft.Azure.Storage.Blob;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	public interface IBlobManager
	{
		Task AppendBytesAsync(string blobName, byte[] buffer, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task AppendStreamAsync(string blobName, Stream stream, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task AppendTextAsync(string blobName, string content, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task CopyBlobAsync(string sourceBlobName, string destinationBlobName, CancellationToken cancellationToken = default);

		Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default);

		Task DeleteBlobsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default);

		Task<byte[]> GetBlobContentAsync(string blobName, CancellationToken cancellationToken = default);

		Task<BlobProperties> GetBlobContentAsync(string blobName, Stream outputStream, CancellationToken cancellationToken = default);

		Task<CloudBlob> GetBlobReferenceAsync(string blobName, CancellationToken cancellationToken = default);

		Task<IEnumerable<IListBlobItem>> ListBlobsAsync(string folder, bool includeSubFolders = false, bool includeMetadata = false, int? maxResults = null, CancellationToken cancellationToken = default);

		Task<IEnumerable<CloudBlobDirectory>> ListSubFoldersAsync(string folder, bool includeMetadata = false, int? maxResults = null, CancellationToken cancellationToken = default);

		Task MoveBlobAsync(string sourceBlobName, string destinationBlobName, CancellationToken cancellationToken = default);

		Task UploadBytesAsync(string blobName, byte[] buffer, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task UploadFileAsync(string blobName, string fileName, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task UploadStreamAsync(string blobName, Stream stream, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		Task UploadTextAsync(string blobName, string content, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);
	}
}
