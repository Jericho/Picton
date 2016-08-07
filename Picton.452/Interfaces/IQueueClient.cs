using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	public interface IQueueClient
	{
		CloudQueue GetQueueReference(string queueName);
		Task<ServiceProperties> GetServicePropertiesAsync(QueueRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<ServiceStats> GetServiceStatsAsync(QueueRequestOptions requestOptions = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		IEnumerable<CloudQueue> ListQueues(string prefix = null, QueueListingDetails queueListingDetails = QueueListingDetails.None, QueueRequestOptions options = null, OperationContext operationContext = null);
		Task<QueueResultSegment> ListQueuesSegmentedAsync(string prefix = null, QueueListingDetails queueListingDetails = QueueListingDetails.None, int? maxResults = default(int?), QueueContinuationToken currentToken = null, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task SetServicePropertiesAsync(ServiceProperties properties, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
	}
}
