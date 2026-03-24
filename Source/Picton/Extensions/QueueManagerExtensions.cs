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
		/// <summary>
		/// Retrieves a single message from the queue asynchronously, making it temporarily invisible to other consumers.
		/// </summary>
		/// <param name="queueManager">The queue manager instance used to access the queue.</param>
		/// <param name="visibilityTimeout">An optional duration specifying how long the retrieved message remains invisible to other consumers. If null, the
		/// default visibility timeout is used.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the first available message from the
		/// queue, or null if the queue is empty.</returns>
		public static async Task<CloudMessage> GetMessageAsync(this QueueManager queueManager, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
		{
			var messages = await queueManager.GetMessagesAsync(1, visibilityTimeout, cancellationToken).ConfigureAwait(false);
			return messages.FirstOrDefault();
		}

		/// <summary>
		/// Retrieves, without removing, the next available message from the queue, if one exists.
		/// </summary>
		/// <remarks>This method does not remove the message from the queue. Use this method to inspect the next
		/// message without dequeuing it.</remarks>
		/// <param name="queueManager">The queue manager instance used to access the queue.</param>
		/// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the next available message in the
		/// queue, or null if the queue is empty.</returns>
		public static async Task<CloudMessage> PeekMessageAsync(this QueueManager queueManager, CancellationToken cancellationToken = default)
		{
			var messages = await queueManager.PeekMessagesAsync(1, cancellationToken).ConfigureAwait(false);
			return messages.FirstOrDefault();
		}
	}
}
