using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using Picton.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	public class QueueClient : IQueueClient
	{
		#region FIELDS

		private readonly CloudQueueClient _cloudQueueClient;

		#endregion

		#region CONSTRUCTOR
		public QueueClient(CloudQueueClient cloudQueueClient)
		{
			_cloudQueueClient = cloudQueueClient;
		}

		public QueueClient(StorageUri storageUri, StorageCredentials credentials)
		{
			_cloudQueueClient = new CloudQueueClient(storageUri, credentials);
		}

		public QueueClient(Uri baseUri, StorageCredentials credentials)
		{
			_cloudQueueClient = new CloudQueueClient(baseUri, credentials);
		}

		#endregion

		#region PUBLIC METHODS

		public CloudQueue GetQueueReference(string queueName)
		{
			return _cloudQueueClient.GetQueueReference(queueName);
		}

		public Task<ServiceProperties> GetServicePropertiesAsync(QueueRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudQueueClient.GetServicePropertiesAsync(requestOptions, operationContext, cancellationToken);
		}

		public Task<ServiceStats> GetServiceStatsAsync(QueueRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudQueueClient.GetServiceStatsAsync(requestOptions, operationContext, cancellationToken);
		}

		public IEnumerable<CloudQueue> ListQueues(string prefix = null, QueueListingDetails queueListingDetails = QueueListingDetails.None, QueueRequestOptions options = null, OperationContext operationContext = null)
		{
			return _cloudQueueClient.ListQueues(prefix, queueListingDetails, options, operationContext);
		}

		public Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix = null, QueueListingDetails queueListingDetails = QueueListingDetails.None, int? maxResults = null, QueueContinuationToken currentToken = null, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudQueueClient.ListQueuesSegmentedAsync(prefix, queueListingDetails, maxResults, currentToken, options, operationContext, cancellationToken);
		}

		public Task SetServicePropertiesAsync(ServiceProperties properties, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken))
		{
			return _cloudQueueClient.SetServicePropertiesAsync(properties, options, operationContext, cancellationToken);
		}

		#endregion
	}
}
