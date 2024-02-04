using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	/// <summary>
	/// Queue manager for multi-tenant scenarios where each tenant has their own dedicated queue.
	/// </summary>
	public interface IMultiTenantQueueManager
	{
		/// <summary>
		/// Add a new message to the back of the tenant queue.
		/// </summary>
		/// <typeparam name="T">The type of the message.</typeparam>
		/// <param name="tenantId">The tenant unique identifier.</param>
		/// <param name="message">The message.</param>
		/// <param name="metadata">Metadata about the message.</param>
		/// <param name="timeToLive">Specifies the time-to-live interval for the message.</param>
		/// <param name="initialVisibilityDelay">Specifies how long the message should be invisible to Dequeue and Peek operations.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task AddMessageAsync<T>(string tenantId, T message, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = default, TimeSpan? initialVisibilityDelay = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Remove all messages from a tenant's queue.
		/// </summary>
		/// <param name="tenantId">The tenant unique identifier.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task ClearTenantAsync(string tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Delete a tenant's queue.
		/// </summary>
		/// <param name="tenantId">The tenant unique identifier.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default);

		/// <summary>
		/// Permanently delete the specified message from the tenant queue.
		/// </summary>
		/// <param name="tenantId">The tenant unique identifier.</param>
		/// <param name="message">The message to delete.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
		/// <returns>A task that represents the asynchronous operation.</returns>
		Task DeleteMessageAsync(string tenantId, CloudMessage message, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieve one or more messages from the front of the tenant queue.
		/// </summary>
		/// <param name="tenantId">The tenant unique identifier.</param>
		/// <param name="maxMessages">
		/// A nonzero integer value that specifies the number of messages to retrieve from the queue, up to a maximum of 32.
		/// If fewer are visible, the visible messages are returned.
		/// By default, a single message is retrieved from the queue with this operation.
		/// </param>
		/// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to server time. The default value is 30 seconds.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
		/// <returns>An array of <see cref="CloudMessage"/>.</returns>
		Task<CloudMessage[]> GetMessagesAsync(string tenantId, int maxMessages = 1, TimeSpan? visibilityTimeout = default, CancellationToken cancellationToken = default);
	}
}
