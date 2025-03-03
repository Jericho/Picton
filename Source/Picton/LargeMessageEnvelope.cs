using System;

namespace Picton
{
	public class LargeMessageEnvelope
	{
		#region PROPERTIES

		public string BlobName { get; internal set; }

		public Version Version { get; set; }

		#endregion
	}
}
