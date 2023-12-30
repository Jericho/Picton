using Azure.Storage.Queues.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	public interface IQueueManager
	{
		string QueueName { get; }

		Task AddMessageAsync<T>(T message, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = default, TimeSpan? initialVisibilityDelay = default, CancellationToken cancellationToken = default);

		Task ClearAsync(CancellationToken cancellationToken = default);

		Task DeleteResourcesAsync(CancellationToken cancellationToken = default);

		Task DeleteMessageAsync(CloudMessage message, CancellationToken cancellationToken = default);

		Task<QueueProperties> GetPropertiesAsync(CancellationToken cancellationToken = default);

		Task<CloudMessage[]> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout = default, CancellationToken cancellationToken = default);

		Task<IEnumerable<QueueSignedIdentifier>> GetAccessPolicyAsync(CancellationToken cancellationToken = default);

		Task<CloudMessage[]> PeekMessagesAsync(int messageCount, CancellationToken cancellationToken = default);

		Task SetMetadataAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken = default);

		Task SetAccessPolicyAsync(IEnumerable<QueueSignedIdentifier> permissions, CancellationToken cancellationToken = default);

		/* Currently, we don't support updating message content due to complexity. See the comment in QueueManager.cs for more details
		Task UpdateMessageAsync(CloudMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default);
		*/

		Task UpdateMessageVisibilityTimeoutAsync(CloudMessage message, TimeSpan visibilityTimeout, CancellationToken cancellationToken = default);

		Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default);
	}
}
