using System;
using System.Collections.Generic;

namespace Picton
{
	/// <summary>
	/// Represents a message stored in a cloud-based queue, including its content, metadata, and queue-related properties.
	/// </summary>
	/// <remarks>Use this class to access message details such as its unique identifier, dequeue count, visibility
	/// timing, and associated metadata. The message content can be of any type, but it is the responsibility of the caller
	/// to ensure it can be serialized and deserialized as required by the queue implementation.</remarks>
	/// <param name="content">The content of the message. This can be any object that represents the data to be stored in the queue.</param>
	public class CloudMessage(object content)
	{
		#region FIELDS

		internal const string LARGE_CONTENT_BLOB_NAME_METADATA = "LargeContentBlobName";
		private IDictionary<string, string> _metadata;

		#endregion

		#region PROPERTIES

		/// <summary>
		/// Gets the number of times the message has been dequeued.
		/// </summary>
		public long DequeueCount { get; internal set; }

		/// <summary>
		/// Gets the time that the message will expire and be automatically deleted.
		/// </summary>
		public DateTimeOffset? ExpiresOn { get; internal set; }

		/// <summary>
		/// Gets the Id of the message.
		/// </summary>
		public string Id { get; internal set; }

		/// <summary>
		/// GEts the time the message was inserted into the queue.
		/// </summary>
		public DateTimeOffset? InsertedOn { get; internal set; }

		/// <summary>
		/// Gets the time that the message will again become visible in the queue.
		/// </summary>
		public DateTimeOffset? NextVisibleOn { get; internal set; }

		/// <summary>
		/// Gets the value that is required to delete the message.
		/// If deletion fails using this popreceipt then the message has been dequeued by another client.
		/// </summary>
		public string PopReceipt { get; internal set; }

		/// <summary>
		/// Gets the content of the message.
		/// </summary>
		public object Content { get; internal set; } = content;

		/// <summary>
		/// Gets or sets the metadata associated with this message.
		/// </summary>
		public IDictionary<string, string> Metadata
		{
			get
			{
				_metadata ??= new Dictionary<string, string>();
				return _metadata;
			}

			set
			{
				_metadata = value;
			}
		}

		#endregion
	}
}
