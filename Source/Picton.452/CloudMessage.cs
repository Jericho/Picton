using System;

namespace Picton
{
	public class CloudMessage
	{
		#region PROPERTIES

		public int DequeueCount { get; internal set; }
		public DateTimeOffset? ExpirationTime { get; internal set; }
		public string Id { get; internal set; }
		public DateTimeOffset? InsertionTime { get; internal set; }
		public DateTimeOffset? NextVisibleTime { get; internal set; }
		public string PopReceipt { get; internal set; }
		public object Content { get; internal set; }
		public Type ContentType { get; internal set; }
		public string LargeContentBlobName { get; internal set; }

		#endregion

		#region CONSTRUCTOR

		public CloudMessage(object content) : this(content, content.GetType())
		{
		}

		public CloudMessage(object content, Type contentType)
		{
			this.Content = content;
			this.ContentType = contentType;
		}

		#endregion
	}
}
