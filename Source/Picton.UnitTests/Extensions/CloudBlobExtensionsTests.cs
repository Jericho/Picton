using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.UnitTests.Extensions
{
	public class CloudBlobExtensionsTests
	{
		private static readonly string BLOB_STORAGE_URL = "http://bogus:10000/devstoreaccount1/";
		private static readonly string BLOB_ITEM_URL = $"{BLOB_STORAGE_URL}MyContainer/Blob.txt";

		[Fact]
		public async Task TryAcquireLeaseAsync_throws_when_blob_is_null()
		{
			// Act
			await Should.ThrowAsync<ArgumentNullException>(() => ((CloudBlob)null).TryAcquireLeaseAsync()).ConfigureAwait(false);
		}

		[Fact]
		public async Task TryAcquireLeaseAsync_throws_when_maxLeaseAttempts_is_too_small()
		{
			// Arrange
			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

			// Act
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlob.Object.TryAcquireLeaseAsync(maxLeaseAttempts: 0)).ConfigureAwait(false);
		}

		[Fact]
		public async Task TryAcquireLeaseAsync_throws_when_maxLeaseAttempts_is_too_large()
		{
			// Arrange
			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

			// Act
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlob.Object.TryAcquireLeaseAsync(maxLeaseAttempts: 11)).ConfigureAwait(false);
		}

		[Fact]
		public async Task TryAcquireLeaseAsync_success()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var expected = "Hello World";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, null, null, null, cancellationToken))
				.ReturnsAsync(expected)
				.Verifiable();

			// Act
			var result = await mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBe(expected);
		}

		[Fact]
		public async Task TryAcquireLeaseAsync_already_leased()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, null, null, null, cancellationToken))
				.ThrowsAsync(new StorageException(new RequestResult() { HttpStatusCode = 409 }, "Already leased", new Exception("???")))
				.Verifiable();

			// Act
			var result = await mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBeNull();
		}

		[Fact]
		public async Task TryAcquireLeaseAsync_throws_when_lease_fails()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var exception = new Exception("An exception occured");

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, null, null, null, cancellationToken))
				.ThrowsAsync(exception)
				.Verifiable();

			// Act
			await Should.ThrowAsync<Exception>(() => mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task TryAcquireLeaseAsync_throws_when_exception_other_than_HTTP409()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var exception = new Exception("An exception occured");

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, null, null, null, cancellationToken))
				.ThrowsAsync(new StorageException(new RequestResult() { HttpStatusCode = 999 }, "Let's simulate some problem", new Exception("???")))
				.Verifiable();

			// Act
			await Should.ThrowAsync<Exception>(() => mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task TryAcquireLeaseAsync_with_retries()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(15);
			var maxRetries = 5;

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(leaseTime, (string)null, null, null, null, cancellationToken))
				.ThrowsAsync(new StorageException(new RequestResult() { HttpStatusCode = 409 }, "Already leased", new Exception("???")));

			// Act
			var result = await mockBlob.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify(c => c.AcquireLeaseAsync(It.IsAny<TimeSpan?>(), It.IsAny<string>(), null, null, null, It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
			result.ShouldBeNull();
		}

		[Fact]
		public async Task TryAcquireLeaseAsync_retries_when_lease_is_blank()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(15);
			var expected = "";
			var maxRetries = 5;

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(leaseTime, (string)null, null, null, null, cancellationToken))
				.Returns(Task.FromResult(expected));

			// Act
			var result = await mockBlob.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify(c => c.AcquireLeaseAsync(It.IsAny<TimeSpan?>(), It.IsAny<string>(), null, null, null, It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
			result.ShouldBe(expected);
		}

		[Fact]
		public async Task AcquireLeaseAsync_default_lease_time()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = (TimeSpan?)null;
			var expected = "Hello World";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), null, null, null, null, cancellationToken))
				.Returns(Task.FromResult(expected))
				.Verifiable();

			// Act
			var result = await mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBe(expected);
		}

		[Fact]
		public async Task AcquireLeaseAsync_10_seconds()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(10);

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

			// Act
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task AcquireLeaseAsync_30_seconds()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(30);
			var expected = "Hello World";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(leaseTime, (string)null, null, null, null, cancellationToken))
				.Returns(Task.FromResult(expected))
				.Verifiable();

			// Act
			var result = await mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBe(expected);
		}

		[Fact]
		public async Task AcquireLeaseAsync_too_long()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromMinutes(2);

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

			// Act
			await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task AcquireLeaseAsync_creates_blob_if_does_not_exist()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(30);
			var expected = "Hello World";

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.SetupSequence(c => c.AcquireLeaseAsync(leaseTime, (string)null, null, null, null, cancellationToken))
				.ThrowsAsync(new StorageException(new RequestResult() { HttpStatusCode = 404 }, "Not found", new Exception("???")))
				.Returns(Task.FromResult(expected));
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), null, null, null, It.IsAny<CancellationToken>()))
				.Returns(Task.FromResult(true));

			// Act
			var result = await mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBe(expected);
		}

		[Fact]
		public void ReleaseLeaseAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			// Act
			Should.Throw<ArgumentNullException>(() => ((CloudBlob)null).ReleaseLeaseAsync(leaseId, cancellationToken));
		}

		[Fact]
		public async Task ReleaseLeaseAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ReleaseLeaseAsync(It.IsAny<AccessCondition>(), null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.ReleaseLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public void RenewLeaseAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			// Act
			Should.Throw<ArgumentNullException>(() => ((CloudBlob)null).RenewLeaseAsync(leaseId, cancellationToken));
		}

		[Fact]
		public async Task RenewLeaseAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.RenewLeaseAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.RenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task TryRenewLeaseAsync_success()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.RenewLeaseAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var result = await mockBlob.Object.TryRenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBeTrue();
		}

		[Fact]
		public async Task TryRenewLeaseAsync_failure()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.RenewLeaseAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), null, null, cancellationToken))
				.Throws(new Exception("Something went wrong"))
				.Verifiable();

			// Act
			var result = await mockBlob.Object.TryRenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBeFalse();
		}

		[Fact]
		public async Task UploadStreamAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			// Act
			await Should.ThrowAsync<ArgumentNullException>(() => ((CloudBlob)null).UploadStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task UploadStreamAsync_throws_when_stream_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var stream = (Stream)null;

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

			// Act
			await Should.ThrowAsync<ArgumentNullException>(() => mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task UploadStreamAsync_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task UploadStreamAsync_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task UploadStreamAsync_to_CloudAppendBlob_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudAppendBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.CreateOrReplaceAsync(null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task UploadStreamAsync_to_CloudAppendBlob_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudAppendBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.CreateOrReplaceAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task UploadStreamAsync_to_CloudPageBlob_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudPageBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task UploadStreamAsync_throws_when_unknown_blob_type()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

			// Act
			var e = await Should.ThrowAsync<Exception>(() => mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);

			// Assert
			e.Message.ShouldStartWith("Unknow blob type: ");
		}

		[Fact]
		public void SetMetadataAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;

			// Act
			Should.Throw<ArgumentNullException>(() => ((CloudBlob)null).SetMetadataAsync(leaseId, cancellationToken));
		}

		[Fact]
		public async Task SetMetadataAsync_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.SetMetadataAsync(It.IsAny<AccessCondition>(), null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.SetMetadataAsync(leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task SetMetadataAsync_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.SetMetadataAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.SetMetadataAsync(leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task DownloadTextAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;

			// Act
			await Should.ThrowAsync<ArgumentNullException>(() => ((CloudBlob)null).DownloadTextAsync(cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task DownloadTextAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var expected = "Hello World";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), null, null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Callback(async (Stream s, AccessCondition ac, BlobRequestOptions o, OperationContext oc, CancellationToken ct) =>
				{
					var buffer = expected.ToBytes();
					await s.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
				})
				.Verifiable();

			// Act
			var result = await mockBlob.Object.DownloadTextAsync(cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBe(expected);
		}

		[Fact]
		public async Task UploadTextAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var content = "Hello World";

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.UploadTextAsync(content, "", cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			// Act
			await Should.ThrowAsync<ArgumentNullException>(() => ((CloudBlob)null).AppendStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task AppendStreamAsync_throws_when_stream_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var stream = (Stream)null;

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

			// Act
			await Should.ThrowAsync<ArgumentNullException>(() => mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);
		}

		[Fact]
		public async Task AppendStreamAsync_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_blob_does_not_exist()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(false))
				.Verifiable();
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_to_CloudAppendBlob_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudAppendBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_to_CloudAppendBlob_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudAppendBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_to_CloudAppendBlob_blob_does_not_exist()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudAppendBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(false))
				.Verifiable();
			mockBlob
				.Setup(c => c.CreateOrReplaceAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_to_CloudPageBlob_without_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudPageBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_to_CloudPageBlob_with_lease()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudPageBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_to_CloudPageBlob_blob_does_not_exist()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudPageBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(false))
				.Verifiable();
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendStreamAsync_throws_when_unknown_blob_type()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			var e = await Should.ThrowAsync<Exception>(() => mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);

			// Assert
			e.Message.ShouldStartWith("Unknow blob type: ");
		}

		[Fact]
		public async Task AppendBytesAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var buffer = "Hello World".ToBytes();

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendBytesAsync(buffer, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task AppendTextAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var content = "Hello World";

			var mockBlob = new Mock<CloudBlockBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.ExistsAsync(null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), It.IsAny<AccessCondition>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();
			mockBlob
				.Setup(c => c.UploadFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				.Returns(Task.FromResult(true))
				.Verifiable();

			// Act
			await mockBlob.Object.AppendTextAsync(content, leaseId, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
		}

		[Fact]
		public async Task DownloadByteArrayAsync()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var expected = "Hello World";

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), null, null, null, cancellationToken))
				.Returns(Task.FromResult(true))
				.Callback(async (Stream s, AccessCondition ac, BlobRequestOptions bro, OperationContext oc, CancellationToken ct) =>
				{
					var buffer = expected.ToBytes();
					await s.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
				})
				.Verifiable();

			// Act
			var result = await mockBlob.Object.DownloadByteArrayAsync(cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBe(expected.ToBytes());
		}

		[Fact]
		public void GetSharedAccessSignatureUri_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var permission = SharedAccessBlobPermissions.Read;
			var duration = TimeSpan.FromMinutes(1);
			var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;

			// Act
			Should.Throw<ArgumentNullException>(() =>
			{
				var result = ((CloudBlob)null).GetSharedAccessSignatureUri(permission, duration, systemClock);
			});
		}

		[Fact]
		public void GetSharedAccessSignatureUri_with_specified_duration()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var permission = SharedAccessBlobPermissions.Read;
			var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;
			var duration = TimeSpan.FromMinutes(1);

			var blob = new CloudBlob(new Uri(BLOB_ITEM_URL), Misc.GetStorageCredentials());

			// Act
			var result = blob.GetSharedAccessSignatureUri(permission, duration, systemClock);

			// Assert
			var qs = ParseQueryString(result);
			qs["st"].ShouldBe("2016-08-12T14:55:00Z");
			qs["se"].ShouldBe("2016-08-12T15:01:00Z");
		}

		[Fact]
		public void GetSharedAccessSignatureUri_default_duration()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var permission = SharedAccessBlobPermissions.Read;
			var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;

			var blob = new CloudBlob(new Uri(BLOB_ITEM_URL), Misc.GetStorageCredentials());

			// Act
			var result = blob.GetSharedAccessSignatureUri(permission, systemClock: systemClock);

			// Assert
			var qs = ParseQueryString(result);
			qs["st"].ShouldBe("2016-08-12T14:55:00Z");
			qs["se"].ShouldBe("2016-08-12T15:15:00Z");
		}

		private static IDictionary<string, string> ParseQueryString(string uri)
		{
			var queryString = uri
				.Split('?')[1]
				.Split('&')
				.ToDictionary(c => c.Split('=')[0], c => Uri.UnescapeDataString(c.Split('=')[1]));
			return queryString;
		}
	}
}
