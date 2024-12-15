using System;
using System.Collections.Generic;

namespace Picton
{
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
