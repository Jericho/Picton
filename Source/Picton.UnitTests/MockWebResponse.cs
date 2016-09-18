using System.Net;
using System.Runtime.Serialization;

namespace Picton.UnitTests
{
	public class MockWebResponse : HttpWebResponse
	{
		public MockWebResponse(SerializationInfo serializationInfo, StreamingContext streamingContext)
#pragma warning disable CS0618 // Type or member is obsolete
			: base(serializationInfo, streamingContext)
#pragma warning restore CS0618 // Type or member is obsolete
		{
		}
	}
}
