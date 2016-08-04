using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Common.Interfaces
{
	public interface IQueueProvider
	{
		Task AddMessageAsync<T>(T message, TimeSpan? timeToLive = default(TimeSpan?), TimeSpan? initialVisibilityDelay = default(TimeSpan?), QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task ClearAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task CreateAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<bool> CreateIfNotExistsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<bool> DeleteIfExistsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task DeleteMessageAsync(CloudMessage message, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<bool> ExistsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task FetchAttributesAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<CloudMessage> GetMessageAsync(TimeSpan? visibilityTimeout = default(TimeSpan?), QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout = default(TimeSpan?), QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		string GetSharedAccessSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, SharedAccessProtocol? protocols = default(SharedAccessProtocol?), IPAddressOrRange ipAddressOrRange = null);
		Task<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task SetMetadataAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
	}
}
