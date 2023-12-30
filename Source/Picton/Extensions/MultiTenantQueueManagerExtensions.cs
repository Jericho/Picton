using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	/// <summary>
	/// Contains extension methods for the <see cref="MultiTenantQueueManager"/> data type.
	/// </summary>
	public static class MultiTenantQueueManagerExtensions
	{
		public static async Task<CloudMessage> GetMessageAsync(this MultiTenantQueueManager queueManager, string tenantId, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
		{
			var messages = await queueManager.GetMessagesAsync(tenantId, 1, visibilityTimeout, cancellationToken).ConfigureAwait(false);
			return messages.FirstOrDefault();
		}
	}
}
