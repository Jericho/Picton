using Azure.Storage.Queues.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Interfaces
{
	/// <summary>
	/// Defines the contract for managing a message queue, including operations for adding, retrieving, peeking, and
	/// deleting messages, as well as managing queue properties, metadata, and access policies.
	/// </summary>
	/// <remarks>Implementations of this interface provide asynchronous methods for interacting with a queue,
	/// supporting scenarios such as message processing, queue maintenance, and access control. Methods are designed to be
	/// used in concurrent and distributed environments. Thread safety and performance considerations may depend on the
	/// specific implementation.</remarks>
	public interface IQueueManager
	{
		/// <summary>Gets the name of the queue.</summary>
		string QueueName { get; }

		/// <summary>
		/// Adds a message to the back of a queue. The visibility timeout specifies how long the message should be invisible
		/// to Dequeue and Peek operations.
		/// </summary>
		/// <typeparam name="T">The type of the message.</typeparam>
		/// <param name="message">The message.</param>
		/// <param name="metadata">Optional. The metatadata.</param>
		/// <param name="timeToLive">Optional. Specifies the time-to-live interval for the message.</param>
		/// <param name="initialVisibilityDelay">Visibility timeout. Optional with a default value of 0. Cannot be larger than 7 days.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>An async task.</returns>
		Task AddMessageAsync<T>(T message, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = default, TimeSpan? initialVisibilityDelay = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Adds multiple messages to the back of a queue. The visibility timeout specifies how long the message should be invisible
		/// to Dequeue and Peek operations.
		/// </summary>
		/// <remarks>
		/// For large number of messages, say more than 500, you need to increase the number of simultanious
		/// connections you allow to a given URI with code similar to this:
		/// <code>
		/// ServicePointManager.DefaultConnectionLimit = 1000;
		/// </code>
		/// </remarks>
		/// <typeparam name="T">The type of the messages.</typeparam>
		/// <param name="messages">The messages.</param>
		/// <param name="metadata">Optional. The metatadata. Please note that all messages with have a copy of this metadata.</param>
		/// <param name="timeToLive">Optional. Specifies the time-to-live interval for the message.</param>
		/// <param name="initialVisibilityDelay">Visibility timeout. Optional with a default value of 0. Cannot be larger than 7 days.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>An async task.</returns>
		Task AddMessagesAsync<T>(IEnumerable<T> messages, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = default, TimeSpan? initialVisibilityDelay = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Clears all messages from the queue.
		/// </summary>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>An async task.</returns>
		Task ClearAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes the queue and all associated resources.
		/// </summary>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>An async task.</returns>
		Task DeleteResourcesAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Deletes the specified message from the queue.
		/// </summary>
		/// <param name="message">The message to delete.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>An async task.</returns>
		Task DeleteMessageAsync(CloudMessage message, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the properties of the queue, including metadata and approximate message count.
		/// </summary>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the queue properties.</returns>
		Task<QueueProperties> GetPropertiesAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieve one or more messages from the front of the queue.
		/// </summary>
		/// <param name="maxMessages">
		/// A nonzero integer value that specifies the number of messages to retrieve from the queue, up to a maximum of 32.
		/// If fewer are visible, the visible messages are returned.
		/// By default, a single message is retrieved from the queue with this operation.
		/// </param>
		/// <param name="visibilityTimeout">Specifies the new visibility timeout value, in seconds, relative to server time. The default value is 30 seconds.</param>
		/// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
		/// <returns>An array of <see cref="CloudMessage"/>.</returns>
		Task<CloudMessage[]> GetMessagesAsync(int maxMessages = 1, TimeSpan? visibilityTimeout = default, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the access policy for the queue, including stored access policies.
		/// </summary>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the collection of signed identifiers.</returns>
		Task<IEnumerable<QueueSignedIdentifier>> GetAccessPolicyAsync(CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves one or more messages from the front of the queue without changing their visibility.
		/// </summary>
		/// <param name="messageCount">The number of messages to peek. Maximum value is 32.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains an array of <see cref="CloudMessage"/>.</returns>
		Task<CloudMessage[]> PeekMessagesAsync(int messageCount, CancellationToken cancellationToken = default);

		/// <summary>
		/// Sets the metadata for the queue.
		/// </summary>
		/// <param name="metadata">The metadata key-value pairs to set on the queue.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>An async task.</returns>
		Task SetMetadataAsync(IDictionary<string, string> metadata, CancellationToken cancellationToken = default);

		/// <summary>
		/// Sets the access policy for the queue, including stored access policies.
		/// </summary>
		/// <param name="permissions">The collection of signed identifiers representing the access policies.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>An async task.</returns>
		Task SetAccessPolicyAsync(IEnumerable<QueueSignedIdentifier> permissions, CancellationToken cancellationToken = default);

		/* Currently, we don't support updating message content due to complexity. See the comment in QueueManager.cs for more details
		Task UpdateMessageAsync(CloudMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options = null, OperationContext operationContext = null, CancellationToken cancellationToken = default);
		*/

		/// <summary>
		/// Updates the visibility timeout of a message in the queue.
		/// </summary>
		/// <param name="message">The message to update.</param>
		/// <param name="visibilityTimeout">The new visibility timeout value.</param>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>An async task.</returns>
		Task UpdateMessageVisibilityTimeoutAsync(CloudMessage message, TimeSpan visibilityTimeout, CancellationToken cancellationToken = default);

		/// <summary>
		/// Retrieves the approximate number of messages in the queue.
		/// </summary>
		/// <param name="cancellationToken">Optional <see cref="CancellationToken"/>.</param>
		/// <returns>A task that represents the asynchronous operation. The task result contains the approximate message count.</returns>
		Task<int> GetApproximateMessageCountAsync(CancellationToken cancellationToken = default);
	}
}
