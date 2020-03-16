using System;

namespace Picton
{
	internal class LargeMessageEnvelope
	{
		#region PROPERTIES

		public string BlobName { get; internal set; }

		public Version Version { get; set; }

		#endregion
	}
}
