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

		public long DequeueCount { get; internal set; }

		public DateTimeOffset? ExpiresOn { get; internal set; }

		public string Id { get; internal set; }

		public DateTimeOffset? InsertedOn { get; internal set; }

		public DateTimeOffset? NextVisibleOn { get; internal set; }

		public string PopReceipt { get; internal set; }

		public object Content { get; internal set; } = content;

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
