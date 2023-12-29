using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	/// <summary>
	/// Contains extension methods for the <see cref="QueueManager"/> data type.
	/// </summary>
	public static class QueueManagerExtensions
	{
		public static async Task<CloudMessage> GetMessageAsync(this QueueManager queueManager, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
		{
			var messages = await queueManager.GetMessagesAsync(1, visibilityTimeout, cancellationToken).ConfigureAwait(false);
			return messages.FirstOrDefault();
		}

		public static async Task<CloudMessage> PeekMessageAsync(this QueueManager queueManager, CancellationToken cancellationToken = default)
		{
			var messages = await queueManager.PeekMessagesAsync(1, cancellationToken).ConfigureAwait(false);
			return messages.FirstOrDefault();
		}
	}
}
