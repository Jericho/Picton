using System;

namespace Picton.Common
{
	internal class MessageContentEnvelope
	{
		#region PROPERTIES

		public object Content { get; internal set; }
		public Type ContentType { get; internal set; }

		#endregion
	}
}
