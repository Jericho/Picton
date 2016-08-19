using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Picton.UnitTests;
using System;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Extensions.UnitTests
{
	[TestClass]
	public class CloudBlobExtensionsTests
	{
		private static readonly string BLOB_STORAGE_URL = "http://bogus:10000/devstoreaccount1/";

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void TryAcquireLeaseAsync_throws_when_blob_is_null()
		{
			var result = ((ICloudBlob)null).TryAcquireLeaseAsync();
			result.Wait();
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void TryAcquireLeaseAsync_throws_when_maxLeaseAttempts_is_too_small()
		{
			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			var result = mockBlob.Object.TryAcquireLeaseAsync(maxLeaseAttempts: 0);
			result.Wait();
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void TryAcquireLeaseAsync_throws_when_maxLeaseAttempts_is_too_large()
		{
			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			var result = mockBlob.Object.TryAcquireLeaseAsync(maxLeaseAttempts: 11);
			result.Wait();
		}

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
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken);
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
			var exception = GetWebException("Already leased", HttpStatusCode.Conflict, true);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, cancellationToken))
				.ThrowsAsync(exception)
				.Verifiable();

			// Act
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			Assert.IsNull(result.Result);
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void TryAcquireLeaseAsync_throws_when_lease_fails()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var exception = new Exception("An exception occured");

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, cancellationToken))
				.ThrowsAsync(exception)
				.Verifiable();

			// Act
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken);
			result.Wait();
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
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken);
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
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		public void TryAcquireLeaseAsync_with_retries()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(15);
			var expected = "Hello World";
			var maxRetries = 5;
			var attempts = 0;

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(leaseTime, (string)null, cancellationToken))
				.Returns(Task.FromResult(expected))
				.Callback(() =>
				{
					attempts++;
					if (attempts < maxRetries)
					{
						var exception = GetWebException("Already leased", HttpStatusCode.Conflict, true);
						throw exception;
					}
				}
			);

			// Act
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify(c => c.AcquireLeaseAsync(It.IsAny<TimeSpan?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
			Assert.AreEqual(expected, result.Result);
		}

		[TestMethod]
		public void TryAcquireLeaseAsync_retries_when_lease_is_blank()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(15);
			var expected = "";
			var maxRetries = 5;

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(leaseTime, (string)null, cancellationToken))
				.Returns(Task.FromResult(expected));

			// Act
			var result = mockBlob.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify(c => c.AcquireLeaseAsync(It.IsAny<TimeSpan?>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
			Assert.AreEqual(expected, result.Result);
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
		[ExpectedException(typeof(AggregateException))]
		public void AcquireLeaseAsync_10_seconds()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(10);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);

			// Act
			var result = mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken);
			result.Wait();
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

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void AcquireLeaseAsync_too_long()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromMinutes(2);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);

			// Act
			var result = mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ReleaseLeaseAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			// Act
			var result = ((ICloudBlob)null).ReleaseLeaseAsync(leaseId, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		public void ReleaseLeaseAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.ReleaseLeaseAsync(It.IsAny<AccessCondition>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.ReleaseLeaseAsync(leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void RenewLeaseAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			// Act
			var result = ((ICloudBlob)null).RenewLeaseAsync(leaseId, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		public void RenewLeaseAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.RenewLeaseAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.RenewLeaseAsync(leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		public void TryRenewLeaseAsync_success()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.RenewLeaseAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.TryRenewLeaseAsync(leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			Assert.IsTrue(result.Result);
		}

		[TestMethod]
		public void TryRenewLeaseAsync_failure()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.RenewLeaseAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), cancellationToken))
				.Throws(new Exception("Something went wrong"))
				.Verifiable();

			// Act
			var result = mockBlob.Object.TryRenewLeaseAsync(leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			Assert.IsFalse(result.Result);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void UploadStreamAsyn_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			// Act
			var result = ((ICloudBlob)null).UploadStreamAsync(stream, leaseId, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void UploadStreamAsync_throws_when_stream_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var stream = (Stream)null;

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);

			// Act
			var result = mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		public void UploadStreamAsync_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		public void UploadStreamAsync_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		public void UploadStreamAsync_to_CloudAppendBlob_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var mockBlobUri = new Uri(BLOB_STORAGE_URL + "test.txt");
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudAppendBlob>(MockBehavior.Strict, mockBlobUri);
			mockBlob
				.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		public void UploadStreamAsync_to_CloudAppendBlob_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var mockBlobUri = new Uri(BLOB_STORAGE_URL + "test.txt");
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudAppendBlob>(MockBehavior.Strict, mockBlobUri);
			mockBlob
				.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void SetMetadataAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;

			// Act
			var result = ((ICloudBlob)null).SetMetadataAsync(leaseId, cancellationToken);
			result.Wait();
		}

		[TestMethod]
		public void SetMetadataAsync_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.SetMetadataAsync(cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.SetMetadataAsync(leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		public void SetMetadataAsync_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.SetMetadataAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.SetMetadataAsync(leaseId, cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		[ExpectedException(typeof(AggregateException))]
		public void DownloadTextAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;

			// Act
			var result = ((ICloudBlob)null).DownloadTextAsync(cancellationToken);
			result.Wait();
		}

		[TestMethod]
		public void DownloadTextAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var expected = "Hello World";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Callback((Stream s, CancellationToken ct) =>
				{
					var buffer = expected.ToBytes();
					s.Write(buffer, 0, buffer.Length);
				})
				.Verifiable();

			// Act
			var result = mockBlob.Object.DownloadTextAsync(cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			Assert.AreEqual(expected, result.Result);
		}

		[TestMethod]
		public void UploadTextAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var content = "Hello World";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = mockBlob.Object.UploadTextAsync(content, "", cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		public void DownloadByteArrayAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var expected = "Hello World";

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Callback((Stream s, CancellationToken ct) =>
				{
					var buffer = expected.ToBytes();
					s.Write(buffer, 0, buffer.Length);
				})
				.Verifiable();

			// Act
			var result = mockBlob.Object.DownloadByteArrayAsync(cancellationToken);
			result.Wait();

			// Assert
			mockBlob.Verify();
			CollectionAssert.AreEqual(expected.ToBytes(), result.Result);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void GetSharedAccessSignatureUri_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var permission = SharedAccessBlobPermissions.Read;
			var duration = TimeSpan.FromMinutes(1);
			var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;

			// Act
			var result = ((ICloudBlob)null).GetSharedAccessSignatureUri(permission, duration, systemClock);
		}

		[TestMethod]
		public void GetSharedAccessSignatureUri_with_specified_duration()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var sas = "abc123";
			var permission = SharedAccessBlobPermissions.Read;
			var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;
			var duration = TimeSpan.FromMinutes(1);
			var startSharingTime = systemClock.UtcNow.AddMinutes(-5);
			var stopSharingTime = systemClock.UtcNow.Add(duration);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.GetSharedAccessSignature(It.Is<SharedAccessBlobPolicy>(p =>
					p.SharedAccessStartTime == startSharingTime &&
					p.SharedAccessExpiryTime == stopSharingTime
				)))
				.Returns(sas)
				.Verifiable();

			mockBlob.SetupGet(b => b.Uri).Returns(new Uri("http://bogus/myaccount/blob"));

			// Act
			var result = mockBlob.Object.GetSharedAccessSignatureUri(permission, duration, systemClock);

			// Assert
			mockBlob.Verify();
		}

		[TestMethod]
		public void GetSharedAccessSignatureUri_default_duration()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var sas = "abc123";
			var permission = SharedAccessBlobPermissions.Read;
			var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;
			var defaultDuration = TimeSpan.FromMinutes(15);
			var startSharingTime = systemClock.UtcNow.AddMinutes(-5);
			var stopSharingTime = systemClock.UtcNow.Add(defaultDuration);

			var mockBlob = new Mock<ICloudBlob>(MockBehavior.Strict);
			mockBlob
				.Setup(c => c.GetSharedAccessSignature(It.Is<SharedAccessBlobPolicy>(p =>
					p.SharedAccessStartTime == startSharingTime &&
					p.SharedAccessExpiryTime == stopSharingTime
				)))
				.Returns(sas)
				.Verifiable();

			mockBlob.SetupGet(b => b.Uri).Returns(new Uri("http://bogus/myaccount/blob"));

			// Act
			var result = mockBlob.Object.GetSharedAccessSignatureUri(permission, systemClock);

			// Assert
			mockBlob.Verify();
		}

		private static WebException GetWebException(string description, HttpStatusCode statusCode, bool includeResponse = true)
		{
			var si = new SerializationInfo(typeof(HttpWebResponse), new FormatterConverter());
			var sc = new StreamingContext();
			var headers = new WebHeaderCollection();
			si.AddValue("m_HttpResponseHeaders", headers);
			si.AddValue("m_Uri", new Uri(BLOB_STORAGE_URL));
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
