using System;
using System.IO;
using System.Net;
using System.Text;

namespace Picton.UnitTests
{
	/// <summary>Provides an implementation of the <see cref="T:System.Net.WebResponse" /> class suitable for mocking.</summary>
	public class MockWebResponse : WebResponse
	{
		private readonly Uri _responseUri;
		private readonly WebHeaderCollection _httpResponseHeaders;
		private readonly string _content;

		public override long ContentLength
		{
			get { return -1; }
		}

		public override string ContentType
		{
			get
			{
				var contentType = _httpResponseHeaders[HttpRequestHeader.ContentType];
				return contentType ?? string.Empty;
			}
		}

		public override WebHeaderCollection Headers
		{
			get
			{
				return _httpResponseHeaders;
			}
		}
		public override Uri ResponseUri
		{
			get { return _responseUri; }
		}

		public override Stream GetResponseStream()
		{
			var stream = new MemoryStream(Encoding.ASCII.GetBytes(_content));
			return (Stream)stream;
		}

		public override bool SupportsHeaders
		{
			get { return true; }
		}

		protected override void Dispose(bool disposing)
		{
			if (!disposing)
			{
				return;
			}
			base.Dispose(true);
		}

		public MockWebResponse(Uri uri, HttpStatusCode statusCode, string content)
		{
			_httpResponseHeaders = new WebHeaderCollection();
			_responseUri = uri;
			_content = content;
		}
	}
}
