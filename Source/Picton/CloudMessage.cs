using System;
using System.Collections.Generic;

namespace Picton
{
	public class CloudMessage
	{
		#region FIELDS

		internal const string LARGE_CONTENT_BLOB_NAME_METADATA = "LargeContentBlobName";
		private IDictionary<string, string> _metadata;

		#endregion

		#region PROPERTIES

		public int DequeueCount { get; internal set; }

		public DateTimeOffset? ExpirationTime { get; internal set; }

		public string Id { get; internal set; }

		public DateTimeOffset? InsertionTime { get; internal set; }

		public DateTimeOffset? NextVisibleTime { get; internal set; }

		public string PopReceipt { get; internal set; }

		public object Content { get; internal set; }

		public IDictionary<string, string> Metadata
		{
			get
			{
				if (_metadata == null) _metadata = new Dictionary<string, string>();
				return _metadata;
			}

			set
			{
				_metadata = value;
			}
		}

		#endregion

		#region CONSTRUCTOR

		public CloudMessage(object content)
		{
			Content = content;
		}

		#endregion
	}
}
