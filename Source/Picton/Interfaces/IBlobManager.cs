using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	public interface IBlobManager
	{
		Task AppendBytesAsync(string blobName, byte[] buffer, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken));
		Task AppendStreamAsync(string blobName, Stream stream, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken));
		Task AppendTextAsync(string blobName, string content, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken));
		Task CopyBlobAsync(string sourceBlobName, string destinationBlobName, CancellationToken cancellationToken = default(CancellationToken));
		Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default(CancellationToken));
		Task DeleteBlobsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default(CancellationToken));
		Task<byte[]> GetBlobContentAsync(string blobName, CancellationToken cancellationToken = default(CancellationToken));
		Task<BlobProperties> GetBlobContentAsync(string blobName, Stream outputStream, CancellationToken cancellationToken = default(CancellationToken));
		Task<CloudBlob> GetBlobReferenceAsync(string blobName, CancellationToken cancellationToken = default(CancellationToken));
		Task<IEnumerable<IListBlobItem>> ListBlobsAsync(string folder, bool includeSubFolders = false, bool includeMetadata = false, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<IEnumerable<CloudBlobDirectory>> ListSubFoldersAsync(string folder, bool includeMetadata = false, int? maxResults = null, CancellationToken cancellationToken = default(CancellationToken));
		Task MoveBlobAsync(string sourceBlobName, string destinationBlobName, CancellationToken cancellationToken = default(CancellationToken));
		Task UploadBytesAsync(string blobName, byte[] buffer, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken));
		Task UploadFileAsync(string blobName, string fileName, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken));
		Task UploadStreamAsync(string blobName, Stream stream, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken));
		Task UploadTextAsync(string blobName, string content, string mimeType = null, NameValueCollection metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default(CancellationToken));
	}
}
