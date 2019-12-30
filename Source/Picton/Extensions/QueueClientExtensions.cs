using Azure;
using Azure.Storage.Queues;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton
{
	/// <summary>
	/// Contains extension methods for the <see cref="QueueClient"/> class.
	/// </summary>
	public static class QueueClientExtensions
	{
		#region PUBLIC EXTENSION METHODS

		/// <summary>
		/// Indicates if a queue exists.
		/// </summary>
		/// <param name="queue">The queue.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the queue exists; false otherwise.</returns>
		public static async Task<bool> ExistsAsync(this QueueClient queue, CancellationToken cancellationToken = default)
		{
			if (queue == null) throw new ArgumentNullException(nameof(queue));

			try
			{
				await queue.GetPropertiesAsync(cancellationToken).ConfigureAwait(false);
				return true;
			}
			catch (RequestFailedException e) when (e.ErrorCode == "QueueNotFound" || e.ErrorCode == "ResourceNotFound")
			{
				return false;
			}
		}

		/// <summary>
		/// Creates a queue if it doesn't already exists.
		/// </summary>
		/// <param name="queue">The queue.</param>
		/// <param name="metadata">The metadata.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the queue was created; false otherwise.</returns>
		public static async Task<bool> CreateIfNotExistsAsync(this QueueClient queue, IDictionary<string, string> metadata = null, CancellationToken cancellationToken = default)
		{
			try
			{
				await queue.CreateAsync(metadata, cancellationToken).ConfigureAwait(false);
				return true;
			}
			catch (RequestFailedException e) when (e.ErrorCode == "QueueAlreadyExists")
			{
				return false;
			}
		}

		/// <summary>
		/// Deletes a queue if it exists.
		/// </summary>
		/// <param name="queue">The queue.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>true if the queue was deleted; false otherwise.</returns>
		public static async Task<bool> DeleteIfExistsAsync(this QueueClient queue, CancellationToken cancellationToken = default)
		{
			try
			{
				await queue.DeleteAsync(cancellationToken).ConfigureAwait(false);
				return true; // True indicates that queue was deleted
			}
			catch (RequestFailedException e) when (e.ErrorCode == "QueueNotFound")
			{
				return false; // False indicates that queue was not deleted
			}
		}

		#endregion
	}
}
