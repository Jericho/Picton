using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
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
		private static readonly IDictionary<string, string> EmptyDictionary = new Dictionary<string, string>();

		/// <summary>
		/// Adds a new message to the back of a queue. The visibility timeout specifies how long the message should be invisible
		/// to Dequeue and Peek operations.
		///
		/// A message must be in a format that can be included in an XML request with UTF-8 encoding.
		/// Otherwise <see cref="QueueClientOptions.MessageEncoding"/> option can be set to <see cref="QueueMessageEncoding.Base64"/> to handle non compliant messages.
		/// The encoded message can be up to 64 KiB in size for versions 2011-08-18 and newer, or 8 KiB in size for previous versions.
		/// </summary>
		/// <remarks>The queue will be created if it doesn't already exist.</remarks>
		/// <param name="queue">The queue client.</param>
		/// <param name="messageText">Message text.</param>
		/// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. Cannot be larger than 7 days.</param>
		/// <param name="timeToLive">Optional. Specifies the time-to-live interval for the message.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="SendReceipt"/>.</returns>
		public static async Task<SendReceipt> SafeSendMessageAsync(this QueueClient queue, string messageText, TimeSpan? visibilityTimeout = default, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
		{
			if (queue == null) throw new ArgumentNullException(nameof(queue));

			Response<SendReceipt> response;
			try
			{
				response = await queue.SendMessageAsync(messageText, visibilityTimeout, timeToLive, cancellationToken = default).ConfigureAwait(false);
			}
			catch (RequestFailedException rfe) when (rfe.ErrorCode == "QueueNotFound")
			{
				await queue.CreateIfNotExistsAsync(QueueClientExtensions.EmptyDictionary, cancellationToken).ConfigureAwait(false);
				response = await queue.SendMessageAsync(messageText, visibilityTimeout, timeToLive, cancellationToken = default).ConfigureAwait(false);
			}

			return response.Value;
		}

		/// <summary>
		/// Adds a new message to the back of a queue. The visibility timeout specifies how long the message should be invisible
		/// to Dequeue and Peek operations.
		///
		/// A message must be in a format that can be included in an XML request with UTF-8 encoding.
		/// Otherwise <see cref="QueueClientOptions.MessageEncoding"/> option can be set to <see cref="QueueMessageEncoding.Base64"/> to handle non compliant messages.
		/// The encoded message can be up to 64 KiB in size for versions 2011-08-18 and newer, or 8 KiB in size for previous versions.
		/// </summary>
		/// <remarks>The queue will be created if it doesn't already exist.</remarks>
		/// <param name="queue">The queue client.</param>
		/// <param name="message">Message.</param>
		/// <param name="visibilityTimeout">Visibility timeout. Optional with a default value of 0. Cannot be larger than 7 days.</param>
		/// <param name="timeToLive">Optional. Specifies the time-to-live interval for the message.</param>
		/// <param name="cancellationToken">The cancellation token.</param>
		/// <returns>The <see cref="SendReceipt"/>.</returns>
		public static async Task<SendReceipt> SafeSendMessageAsync(this QueueClient queue, BinaryData message, TimeSpan? visibilityTimeout = default, TimeSpan? timeToLive = default, CancellationToken cancellationToken = default)
		{
			if (queue == null) throw new ArgumentNullException(nameof(queue));

			Response<SendReceipt> response;
			try
			{
				response = await queue.SendMessageAsync(message, visibilityTimeout, timeToLive, cancellationToken = default).ConfigureAwait(false);
			}
			catch (RequestFailedException rfe) when (rfe.ErrorCode == "QueueNotFound")
			{
				await queue.CreateIfNotExistsAsync(QueueClientExtensions.EmptyDictionary, cancellationToken).ConfigureAwait(false);
				response = await queue.SendMessageAsync(message, visibilityTimeout, timeToLive, cancellationToken = default).ConfigureAwait(false);
			}

			return response.Value;
		}
	}
}
