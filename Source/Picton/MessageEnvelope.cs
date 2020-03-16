using System;
using System.Collections.Generic;

namespace Picton
{
	internal class MessageEnvelope
	{
		#region PROPERTIES

		public Version Version { get; set; }

		public object Content { get; set; }

		public IDictionary<string, string> Metadata { get; set; }

		#endregion
	}
}
