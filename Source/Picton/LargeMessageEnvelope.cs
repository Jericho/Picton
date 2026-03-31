using MessagePack;
using System;

namespace Picton
{
	/// <summary>
	/// Represents an envelope for transporting large messages by referencing their storage location and version
	/// information.
	/// </summary>
	/// <remarks>
	/// This class is used internally by the Picton library and it's not intended to be used by developers.
	/// It would make sense to keep this class 'internal' but it needs to be 'public' for MessagePack to be able
	/// to serialize and deserialize it when running on Linux under .NET full framework.
	/// </remarks>
	[MessagePackObject(AllowPrivate = true)]
	public class LargeMessageEnvelope
	{
		#region PROPERTIES

		/// <summary>
		/// Gets or sets the name of the blob where the content of the message is available.
		/// </summary>
		[Key(0)]
		public string BlobName { get; set; }

		/// <summary>
		/// Gets or sets the version of the Picton library that was used to send the message.
		/// </summary>
		[Key(1)]
		public Version Version { get; set; }

		#endregion
	}
}
