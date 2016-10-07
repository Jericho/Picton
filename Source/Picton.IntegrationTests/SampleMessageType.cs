using System;

namespace Picton.IntegrationTests
{
	public class SampleMessageType
	{
		public string StringProp { get; set; }      //using the text "hello"
		public int IntProp { get; set; }
		public Guid GuidProp { get; set; }
		public DateTime DateProp { get; set; }
	}
}
