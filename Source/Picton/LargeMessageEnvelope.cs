using MessagePack;
using System;

namespace Picton
{
	[MessagePackObject(AllowPrivate = true)]
	internal class LargeMessageEnvelope
	{
		#region PROPERTIES

		[Key(0)]
		public string BlobName { get; internal set; }

		[Key(1)]
		public Version Version { get; set; }

		#endregion
	}
}
