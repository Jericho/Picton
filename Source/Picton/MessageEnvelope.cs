using MessagePack;
using System;
using System.Collections.Generic;

namespace Picton
{
	/// <summary>
	/// Represents a container for a message, including its version, content, and associated metadata.
	/// </summary>
	/// <remarks>
	/// This class is used internally by the Picton library and it's not intended to be used by developers.
	/// It would make sense to keep this class 'internal' but it needs to be 'public' for MessagePack to be able
	/// to serialize and deserialize it when running on Linux under .NET full framework.
	/// </remarks>
	[MessagePackObject(AllowPrivate = true)]
	public class MessageEnvelope
	{
		#region PROPERTIES

		/// <summary>
		/// Gets or sets the version of the Picton library that was used to send the message.
		/// </summary>
		[Key(0)]
		public Version Version { get; set; }

		/// <summary>
		/// Gets or sets the message content.
		/// </summary>
		[Key(1)]
		public object Content { get; set; }

		/// <summary>
		/// Gets or sets the collection of metadata associated with the message.
		/// </summary>
		[Key(2)]
		public IDictionary<string, string> Metadata { get; set; }

		#endregion
	}
}
