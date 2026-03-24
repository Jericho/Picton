using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace Picton.Managers
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
	/// <summary>
	/// Contains extension methods for the <see cref="MultiTenantQueueManager"/> data type.
	/// </summary>
	public static class MultiTenantQueueManagerExtensions
	{
		/// <summary>
		/// Retrieves a single message from the specified tenant's queue asynchronously.
		/// </summary>
		/// <param name="queueManager">The multi-tenant queue manager instance used to access the tenant's queue.</param>
		/// <param name="tenantId">The identifier of the tenant whose queue to retrieve the message from. Cannot be null.</param>
		/// <param name="visibilityTimeout">An optional time interval during which the retrieved message will be invisible to other consumers. If null, the
		/// default visibility timeout is used.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the first available message from the
		/// tenant's queue, or null if no messages are available.</returns>
		public static async Task<CloudMessage> GetMessageAsync(this MultiTenantQueueManager queueManager, string tenantId, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
		{
			var messages = await queueManager.GetMessagesAsync(tenantId, 1, visibilityTimeout, cancellationToken).ConfigureAwait(false);
			return messages.FirstOrDefault();
		}
	}
}
