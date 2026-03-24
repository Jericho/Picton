using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	/// <summary>
	/// Defines a set of operations for managing binary large objects (blobs) in a storage system, including uploading,
	/// downloading, appending, copying, moving, deleting, and listing blobs.
	/// </summary>
	/// <remarks>The interface provides asynchronous methods for common blob storage operations, supporting a variety
	/// of data types and advanced options such as metadata, content settings, and lease acquisition for concurrency
	/// control. Implementations are expected to handle blob storage backends and may enforce specific constraints or
	/// behaviors related to the underlying storage provider. Thread safety and performance characteristics depend on the
	/// concrete implementation.</remarks>
	public interface IBlobManager
	{
		/// <summary>
		/// Appends the specified byte array to the end of the blob with the given name asynchronously, creating the blob if it
		/// does not exist.
		/// </summary>
		/// <remarks>If acquireLease is true, the method will attempt to acquire a lease on the blob before appending
		/// data. If the lease cannot be acquired after the specified number of attempts, the operation will fail. This method
		/// is thread-safe and can be used concurrently for different blobs.</remarks>
		/// <param name="blobName">The name of the blob to which the content will be appended. Cannot be null or empty.</param>
		/// <param name="content">The byte array containing the data to append to the blob. Cannot be null.</param>
		/// <param name="metadata">An optional dictionary of metadata key-value pairs to associate with the blob. If null, no metadata is set or
		/// updated.</param>
		/// <param name="acquireLease">true to attempt to acquire a lease on the blob before appending; otherwise, false. Acquiring a lease can help
		/// prevent concurrent modifications.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease if acquireLease is true. Must be greater than zero.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous append operation.</returns>
		Task AppendBytesAsync(string blobName, byte[] content, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		/// <summary>
		/// Appends the contents of the specified stream to the end of the blob with the given name asynchronously.
		/// </summary>
		/// <remarks>If acquireLease is set to true, the method will attempt to acquire a lease on the blob before
		/// appending data. If the lease cannot be acquired after the specified number of attempts, the operation will fail.
		/// The method does not close or dispose the provided stream.</remarks>
		/// <param name="blobName">The name of the blob to which the stream will be appended. Cannot be null or empty.</param>
		/// <param name="stream">The stream containing the data to append to the blob. The stream must be readable and positioned at the start of
		/// the data to append.</param>
		/// <param name="metadata">An optional dictionary of metadata to associate with the blob. If null, no metadata is set or updated.</param>
		/// <param name="acquireLease">true to acquire a lease on the blob before appending; otherwise, false. Acquiring a lease can help prevent
		/// concurrent modifications.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease if requested. Must be greater than zero.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous append operation.</returns>
		Task AppendStreamAsync(string blobName, Stream stream, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		/// <summary>
		/// Appends the specified text content to the end of the blob identified by the given name asynchronously.
		/// </summary>
		/// <remarks>If acquireLease is true, the method will attempt to acquire a lease on the blob before appending
		/// content. If the lease cannot be acquired after the specified number of attempts, the operation will fail. This
		/// method does not create the blob if it does not exist.</remarks>
		/// <param name="blobName">The name of the blob to which the content will be appended. Cannot be null or empty.</param>
		/// <param name="content">The text content to append to the blob. Cannot be null.</param>
		/// <param name="metadata">An optional dictionary of metadata key-value pairs to associate with the blob. If null, no metadata is set or
		/// updated.</param>
		/// <param name="acquireLease">true to acquire a lease on the blob before appending; otherwise, false. Acquiring a lease can help prevent
		/// concurrent modifications.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease if acquireLease is true. Must be greater than zero.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous append operation.</returns>
		Task AppendTextAsync(string blobName, string content, IDictionary<string, string> metadata = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously copies a blob to a new location within the same storage container.
		/// </summary>
		/// <remarks>If acquireLease is set to true, the method will attempt to acquire a lease on the source blob
		/// before copying. The operation may retry up to maxLeaseAttempts times if lease acquisition fails. The method does
		/// not return until the copy operation is complete or the cancellation token is triggered.</remarks>
		/// <param name="sourceBlobName">The name of the source blob to copy. Cannot be null or empty.</param>
		/// <param name="destinationBlobName">The name of the destination blob where the source will be copied. Cannot be null or empty.</param>
		/// <param name="acquireLease">true to acquire a lease on the source blob before copying; otherwise, false.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease on the source blob. Must be greater than zero.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous copy operation.</returns>
		Task CopyBlobAsync(string sourceBlobName, string destinationBlobName, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes the specified blob asynchronously from the storage container.
		/// </summary>
		/// <param name="blobName">The name of the blob to delete. Cannot be null or empty.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
		/// <returns>A task that represents the asynchronous delete operation.</returns>
		Task DeleteBlobAsync(string blobName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes all blobs whose names begin with the specified prefix asynchronously.
		/// </summary>
		/// <remarks>Use this method to remove multiple blobs in bulk based on a common naming pattern. The operation
		/// is performed asynchronously and may take time depending on the number of matching blobs.</remarks>
		/// <param name="prefix">The prefix to match blob names against. All blobs with names that start with this prefix will be deleted. Cannot
		/// be null or empty.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the delete operation.</param>
		/// <returns>A task that represents the asynchronous delete operation.</returns>
		Task DeleteBlobsWithPrefixAsync(string prefix, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves the binary content of the specified blob as a byte array.
		/// </summary>
		/// <param name="blobName">The name of the blob to retrieve. Cannot be null or empty.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the download operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the blob content as a byte array.</returns>
		Task<byte[]> GetBlobBinaryContentAsync(string blobName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously retrieves the content and metadata of the specified blob.
		/// </summary>
		/// <param name="blobName">The name of the blob to retrieve. Cannot be null or empty.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains a BlobDownloadInfo object with the
		/// blob's content and metadata.</returns>
		Task<BlobDownloadInfo> GetBlobContentAsync(string blobName, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves a reference to a blob with the specified name within the container.
		/// </summary>
		/// <param name="blobName">The name of the blob to reference. Cannot be null or empty.</param>
		/// <returns>A BlobClient instance representing the specified blob. The returned client can be used to perform operations on
		/// the blob.</returns>
		BlobClient GetBlobReference(string blobName);

		/// <summary>
		/// Enumerates the blobs in the container, optionally filtering by prefix and including blob metadata.
		/// </summary>
		/// <remarks>Enumeration is performed lazily and may make multiple service requests as pages are retrieved.
		/// The returned collection can be iterated synchronously or asynchronously.</remarks>
		/// <param name="prefix">A string that filters the results to return only blobs whose names begin with the specified prefix. If null or
		/// empty, all blobs are returned.</param>
		/// <param name="includeMetadata">true to include blob metadata in the results; otherwise, false.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. The enumeration is canceled if the token is triggered.</param>
		/// <returns>A pageable collection of BlobItem objects representing the blobs in the container. The collection may be empty if
		/// no blobs match the specified criteria.</returns>
		Pageable<BlobItem> ListBlobs(string prefix, bool includeMetadata = false, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously moves a blob from the specified source name to the specified destination name within the storage
		/// container.
		/// </summary>
		/// <remarks>If acquireLease is set to true, the method will attempt to acquire a lease on the source blob
		/// before moving it. The operation may fail if the lease cannot be acquired within the specified number of attempts.
		/// The move operation is not atomic; the source blob is copied to the destination and then deleted.</remarks>
		/// <param name="sourceBlobName">The name of the source blob to move. Cannot be null or empty.</param>
		/// <param name="destinationBlobName">The name to assign to the destination blob. Cannot be null or empty.</param>
		/// <param name="acquireLease">true to acquire a lease on the source blob before moving; otherwise, false. Acquiring a lease can help prevent
		/// concurrent modifications during the move operation.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease on the source blob if acquireLease is true. Must be greater than
		/// zero.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
		/// <returns>A task that represents the asynchronous move operation.</returns>
		Task MoveBlobAsync(string sourceBlobName, string destinationBlobName, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously uploads a byte array to the specified blob, optionally setting metadata, content type, cache
		/// control, and content encoding properties.
		/// </summary>
		/// <remarks>If acquireLease is set to true, the method will attempt to acquire a lease on the blob before
		/// uploading. The operation is performed asynchronously and may be canceled using the provided cancellation
		/// token.</remarks>
		/// <param name="blobName">The name of the blob to which the data will be uploaded. Cannot be null or empty.</param>
		/// <param name="buffer">The byte array containing the data to upload. Cannot be null.</param>
		/// <param name="mimeType">The MIME type to associate with the blob. If null, a default type may be used.</param>
		/// <param name="metadata">An optional dictionary of metadata key-value pairs to associate with the blob. If null, no metadata is set.</param>
		/// <param name="cacheControl">An optional cache control header value to set on the blob. If null, no cache control is applied.</param>
		/// <param name="contentEncoding">An optional content encoding value to set on the blob. If null, no content encoding is applied.</param>
		/// <param name="acquireLease">true to acquire a lease on the blob before uploading; otherwise, false.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease if acquireLease is true. Must be greater than zero.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the upload operation.</param>
		/// <returns>A task that represents the asynchronous upload operation.</returns>
		Task UploadBytesAsync(string blobName, byte[] buffer, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously uploads a file to the specified blob, applying optional metadata, content type, and upload settings.
		/// </summary>
		/// <remarks>If a lease is requested and cannot be acquired within the specified number of attempts, the upload
		/// will not proceed. The method does not overwrite existing metadata unless new values are provided.</remarks>
		/// <param name="blobName">The name of the destination blob to which the file will be uploaded. Cannot be null or empty.</param>
		/// <param name="fileName">The path to the local file to upload. Must refer to an existing file.</param>
		/// <param name="mimeType">The MIME type to associate with the uploaded blob. If null, a default type may be used.</param>
		/// <param name="metadata">A dictionary of user-defined metadata to associate with the blob. May be null if no metadata is required.</param>
		/// <param name="cacheControl">The cache control header value to set on the blob. If null, no cache control header is set.</param>
		/// <param name="contentEncoding">The content encoding to set on the blob, such as 'gzip'. If null, no content encoding is set.</param>
		/// <param name="acquireLease">true to acquire a lease on the blob before uploading; otherwise, false.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease if requested. Must be greater than zero.</param>
		/// <param name="cancellationToken">A token to monitor for cancellation requests. The operation is canceled if the token is triggered.</param>
		/// <returns>A task that represents the asynchronous upload operation.</returns>
		Task UploadFileAsync(string blobName, string fileName, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously uploads the provided stream to the specified blob, applying optional metadata and content settings.
		/// </summary>
		/// <remarks>If acquireLease is set to true, the method will attempt to acquire a lease on the blob before
		/// uploading. The operation may retry lease acquisition up to maxLeaseAttempts times. The caller is responsible for
		/// ensuring that the stream remains open and readable for the duration of the upload.</remarks>
		/// <param name="blobName">The name of the blob to which the stream will be uploaded. Cannot be null or empty.</param>
		/// <param name="stream">The stream containing the data to upload. The stream must be readable and positioned at the start of the data to
		/// upload.</param>
		/// <param name="mimeType">The MIME type to associate with the blob. If null, a default content type may be used.</param>
		/// <param name="metadata">An optional dictionary of metadata key-value pairs to associate with the blob. Keys and values must conform to
		/// storage service requirements.</param>
		/// <param name="cacheControl">An optional cache control directive to set on the blob. If null, no cache control header is set.</param>
		/// <param name="contentEncoding">An optional content encoding value to set on the blob. If null, no content encoding is set.</param>
		/// <param name="acquireLease">true to acquire a lease on the blob before uploading; otherwise, false.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease if requested. Must be greater than zero.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the upload operation.</param>
		/// <returns>A task that represents the asynchronous upload operation.</returns>
		Task UploadStreamAsync(string blobName, Stream stream, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);

		/// <summary>
		/// Asynchronously uploads the specified text content to a blob with the given name, applying optional metadata and
		/// content settings.
		/// </summary>
		/// <remarks>If a lease is requested and cannot be acquired within the specified number of attempts, the
		/// operation may fail. The method does not overwrite existing metadata unless explicitly provided.</remarks>
		/// <param name="blobName">The name of the blob to which the content will be uploaded. Cannot be null or empty.</param>
		/// <param name="content">The text content to upload to the blob. Cannot be null.</param>
		/// <param name="mimeType">The MIME type to associate with the blob. If null, a default type may be used.</param>
		/// <param name="metadata">An optional dictionary of metadata key-value pairs to associate with the blob. If null, no metadata is set.</param>
		/// <param name="cacheControl">An optional cache control header value to set for the blob. If null, no cache control is applied.</param>
		/// <param name="contentEncoding">An optional content encoding value to set for the blob. If null, no content encoding is applied.</param>
		/// <param name="acquireLease">true to acquire a lease on the blob before uploading; otherwise, false.</param>
		/// <param name="maxLeaseAttempts">The maximum number of attempts to acquire a lease if requested. Must be greater than zero.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the upload operation.</param>
		/// <returns>A task that represents the asynchronous upload operation.</returns>
		Task UploadTextAsync(string blobName, string content, string mimeType = null, IDictionary<string, string> metadata = null, string cacheControl = null, string contentEncoding = null, bool acquireLease = false, int maxLeaseAttempts = 1, CancellationToken cancellationToken = default);
	}
}
