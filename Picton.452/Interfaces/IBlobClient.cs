using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	public interface IBlobClient
	{
		Task<ICloudBlob> GetBlobReferenceFromServerAsync(StorageUri blobUri, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		CloudBlobContainer GetContainerReference(string containerName);
		CloudBlobContainer GetRootContainerReference();
		Task<ServiceProperties> GetServicePropertiesAsync(BlobRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<ServiceStats> GetServiceStatsAsync(BlobRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken = default(CancellationToken));
		IEnumerable<IListBlobItem> ListBlobs(string prefix, bool useFlatBlobListing = false, BlobListingDetails blobListingDetails = BlobListingDetails.None, BlobRequestOptions options = null, OperationContext operationContext = null);
		Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails = BlobListingDetails.None, int? maxResults = default(int?), BlobContinuationToken currentToken = null, BlobRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		IEnumerable<CloudBlobContainer> ListContainers(string prefix = null, ContainerListingDetails detailsIncluded = ContainerListingDetails.None, BlobRequestOptions options = null, OperationContext operationContext = null);
		Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix = null, ContainerListingDetails detailsIncluded = ContainerListingDetails.None, int? maxResults = default(int?), BlobContinuationToken continuationToken = null, BlobRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task SetServicePropertiesAsync(ServiceProperties properties, BlobRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
	}
}