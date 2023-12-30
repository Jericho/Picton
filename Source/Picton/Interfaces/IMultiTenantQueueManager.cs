using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	public interface IMultiTenantQueueManager
	{
		Task AddMessageAsync<T>(string tenantId, T message, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = default, TimeSpan? initialVisibilityDelay = default, CancellationToken cancellationToken = default);

		Task ClearTenantAsync(string tenantId, CancellationToken cancellationToken = default);

		Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default);

		Task DeleteMessageAsync(string tenantId, CloudMessage message, CancellationToken cancellationToken = default);

		Task<CloudMessage[]> GetMessagesAsync(string tenantId, int messageCount, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);
	}
}
