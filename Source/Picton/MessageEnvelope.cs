using MessagePack;
using System;
using System.Collections.Generic;

namespace Picton
{
	[MessagePackObject(false, AllowPrivate = true, SuppressSourceGeneration = true)]
	internal class MessageEnvelope
	{
		#region PROPERTIES

		[Key(0)]
		public Version Version { get; set; }

		[Key(1)]
		public object Content { get; set; }

		[Key(2)]
		public IDictionary<string, string> Metadata { get; set; }

		#endregion
	}
}
