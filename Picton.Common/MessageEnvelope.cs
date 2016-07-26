using System;

namespace Picton.Common
{
	internal class MessageEnvelope
	{
		#region PROPERTIES

		public object Payload { get; internal set; }
		public Type PayloadType { get; internal set; }

		#endregion
	}
}
