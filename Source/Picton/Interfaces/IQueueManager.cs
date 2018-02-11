using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	public interface IQueueManager
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

		Task<IEnumerable<CloudMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout = default(TimeSpan?), QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));

		Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));

		string GetSharedAccessSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, SharedAccessProtocol? protocols = default(SharedAccessProtocol?), IPAddressOrRange ipAddressOrRange = null);

		Task<CloudMessage> PeekMessageAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));

		Task<IEnumerable<CloudMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));

		Task SetMetadataAsync(QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));

		Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));

		/* Currently, we don't support updating message content due to complexity. See the coment in QueueManager.cs for more details
		Task UpdateMessageAsync(CloudMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));
		*/

		Task UpdateMessageVisibilityTimeoutAsync(CloudMessage message, TimeSpan visibilityTimeout, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default(CancellationToken));

		Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default(CancellationToken));
	}
}
