using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Picton.UnitTests;
using System;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Extensions.UnitTests
{
	[TestClass]
	public class CloudBlobExtensionsTests
	{
		[TestMethod]
		public void TryAcquireLeaseAsync_success()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var expected = "Hello World";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, cancellationToken))
				.Returns(Task.FromResult(expected))
				.Verifiable();

			// Act
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			Assert.AreEqual(expected, result.Result);
		}

		[TestMethod]
		public void TryAcquireLeaseAsync_already_leased()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var exception = GetWebException("Already leased", HttpStatusCode.Conflict);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, cancellationToken))
				.ThrowsAsync(exception)
				.Verifiable();

			// Act
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			Assert.IsNull(result.Result);
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void TryAcquireLeaseAsync_fail_with_response()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var exception = GetWebException("Some error ocured", HttpStatusCode.BadRequest);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, cancellationToken))
				.ThrowsAsync(exception)
				.Verifiable();

			// Act
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void TryAcquireLeaseAsync_fail_without_response()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var exception = GetWebException("Some error ocured", HttpStatusCode.BadRequest, false);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, cancellationToken))
				.ThrowsAsync(exception)
				.Verifiable();

			// Act
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		public void AcquireLeaseAsync_default_lease_time()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var expected = "Hello World";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, cancellationToken))
				.Returns(Task.FromResult(expected))
				.Verifiable();

			// Act
			var result = mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			Assert.AreEqual(expected, result.Result);
		}

		[TestMethod]
		public void AcquireLeaseAsync_30_seconds()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(30);
			var expected = "Hello World";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(leaseTime, (string)null, cancellationToken))
				.Returns(Task.FromResult(expected))
				.Verifiable();

			// Act
			var result = mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			Assert.AreEqual(expected, result.Result);
		}

		private static WebException GetWebException(string description, HttpStatusCode statusCode, bool includeResponse = true)
		{
			var si = new SerializationInfo(typeof(HttpWebResponse), new FormatterConverter());
			var sc = new StreamingContext();
			var headers = new WebHeaderCollection();
			si.AddValue("m_HttpResponseHeaders", headers);
			si.AddValue("m_Uri", new Uri("http://bogus/blob"));
			si.AddValue("m_Certificate", null);
			si.AddValue("m_Version", HttpVersion.Version11);
			si.AddValue("m_StatusCode", statusCode);
			si.AddValue("m_ContentLength", 0);
			si.AddValue("m_Verb", "GET");
			si.AddValue("m_StatusDescription", description);
			si.AddValue("m_MediaType", null);
			var wr = includeResponse ? new MockWebResponse(si, sc) : (WebResponse)null;
			var inner = new Exception(description);
			return new WebException("This request failed", inner, WebExceptionStatus.ProtocolError, wr);
		}
	}
}
