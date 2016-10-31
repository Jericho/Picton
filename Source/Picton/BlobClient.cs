using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Picton.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	public class BlobClient : IBlobClient
	{
		#region FIELDS

		private readonly CloudBlobClient _cloudBlobClient;

		#endregion

		#region CONSTRUCTOR
		public BlobClient(CloudBlobClient cloudBlobClient)
		{
			_cloudBlobClient = cloudBlobClient;
		}

		[ExcludeFromCodeCoverage]
		public BlobClient(Uri baseUri)
		{
			_cloudBlobClient = new CloudBlobClient(baseUri);
		}

		[ExcludeFromCodeCoverage]
		public BlobClient(StorageUri storageUri, StorageCredentials credentials)
		{
			_cloudBlobClient = new CloudBlobClient(storageUri, credentials);
		}

		[ExcludeFromCodeCoverage]
		public BlobClient(Uri baseUri, StorageCredentials credentials)
		{
			_cloudBlobClient = new CloudBlobClient(baseUri, credentials);
		}

		#endregion

		#region STATIC METHODS

		public static BlobClient FromCloudBlobClient(CloudBlobClient cloudBlobClient)
		{
			if (cloudBlobClient == null) return null;
			return new BlobClient(cloudBlobClient);
		}

		#endregion

		#region PUBLIC METHODS

		public Task<ICloudBlob> GetBlobReferenceFromServerAsync(StorageUri blobUri, AccessCondition accessCondition = null, BlobRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudBlobClient.GetBlobReferenceFromServerAsync(blobUri, accessCondition, options, operationContext, cancellationToken);
		}

		public CloudBlobContainer GetContainerReference(string containerName)
		{
			return _cloudBlobClient.GetContainerReference(containerName);
		}

		public CloudBlobContainer GetRootContainerReference()
		{
			return _cloudBlobClient.GetRootContainerReference();
		}

		public Task<ServiceProperties> GetServicePropertiesAsync(BlobRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudBlobClient.GetServicePropertiesAsync(requestOptions, operationContext, cancellationToken);
		}

		public Task<ServiceStats> GetServiceStatsAsync(BlobRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudBlobClient.GetServiceStatsAsync(requestOptions, operationContext, cancellationToken);
		}

		public IEnumerable<IListBlobItem> ListBlobs(string prefix, bool useFlatBlobListing = false, BlobListingDetails blobListingDetails = BlobListingDetails.None, BlobRequestOptions options = null, OperationContext operationContext = null)
		{
			return _cloudBlobClient.ListBlobs(prefix, useFlatBlobListing, blobListingDetails, options, operationContext);
		}

		public Task<BlobResultSegment> ListBlobsSegmentedAsync(string prefix, bool useFlatBlobListing, BlobListingDetails blobListingDetails = BlobListingDetails.None, int? maxResults = null, BlobContinuationToken currentToken = null, BlobRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudBlobClient.ListBlobsSegmentedAsync(prefix, useFlatBlobListing, blobListingDetails, maxResults, currentToken, options, operationContext, cancellationToken);
		}

		public IEnumerable<CloudBlobContainer> ListContainers(string prefix = null, ContainerListingDetails detailsIncluded = ContainerListingDetails.None, BlobRequestOptions options = null, OperationContext operationContext = null)
		{
			return _cloudBlobClient.ListContainers(prefix, detailsIncluded, options, operationContext);
		}

		public Task<ContainerResultSegment> ListContainersSegmentedAsync(string prefix = null, ContainerListingDetails detailsIncluded = ContainerListingDetails.None, int? maxResults = null, BlobContinuationToken continuationToken = null, BlobRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudBlobClient.ListContainersSegmentedAsync(prefix, detailsIncluded, maxResults, continuationToken, options, operationContext, cancellationToken);
		}

		public Task SetServicePropertiesAsync(ServiceProperties properties, BlobRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudBlobClient.SetServicePropertiesAsync(properties, requestOptions, operationContext, cancellationToken);
		}

		#endregion
	}
}
