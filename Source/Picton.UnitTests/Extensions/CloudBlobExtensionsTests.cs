using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Moq;
using Picton.UnitTests;
using Shouldly;
using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.Extensions.UnitTests
{
	public class CloudBlobExtensionsTests
	{
		private static readonly string BLOB_STORAGE_URL = "http://bogus:10000/devstoreaccount1/";
		private static readonly string BLOB_ITEM_URL = $"{BLOB_STORAGE_URL}MyContainer/Blob.txt";

		[Fact]
		public void TryAcquireLeaseAsync_throws_when_blob_is_null()
		{
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await ((CloudBlob)null).TryAcquireLeaseAsync().ConfigureAwait(false);
			});
		}

		[Fact]
		public void TryAcquireLeaseAsync_throws_when_maxLeaseAttempts_is_too_small()
		{
			Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			{
				var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				await mockBlob.Object.TryAcquireLeaseAsync(maxLeaseAttempts: 0).ConfigureAwait(false);
			});
		}

		[Fact]
		public void TryAcquireLeaseAsync_throws_when_maxLeaseAttempts_is_too_large()
		{
			Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			{
				var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				await mockBlob.Object.TryAcquireLeaseAsync(maxLeaseAttempts: 11).ConfigureAwait(false);
			});
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
			var exception = new StorageException(new RequestResult() { HttpStatusCode = 409 }, "Already leased", new Exception("???"));

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
			mockBlob
				.Setup(c => c.AcquireLeaseAsync(TimeSpan.FromSeconds(15), (string)null, null, null, null, cancellationToken))
				.ThrowsAsync(exception)
				.Verifiable();

			// Act
			var result = await mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken).ConfigureAwait(false);

			// Assert
			mockBlob.Verify();
			result.ShouldBeNull();
		}

		[Fact]
		public void TryAcquireLeaseAsync_throws_when_lease_fails()
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
			Should.ThrowAsync<Exception>(async () =>
			{
				await mockBlob.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken).ConfigureAwait(false);
			});
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
			await Should.ThrowAsync<StorageException>(async () =>
			{
				var result = await mockBlob.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken).ConfigureAwait(false);
			});

			// Assert
			mockBlob.Verify(c => c.AcquireLeaseAsync(It.IsAny<TimeSpan?>(), It.IsAny<string>(), null, null, null, It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
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
		public void AcquireLeaseAsync_10_seconds()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromSeconds(10);

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

			// Act
			Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			{
				await mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);
			});
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
		public void AcquireLeaseAsync_too_long()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseTime = TimeSpan.FromMinutes(2);

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict);

			// Act
			Should.ThrowAsync<ArgumentOutOfRangeException>(async () =>
			{
				await mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);
			});
		}

		[Fact]
		public void ReleaseLeaseAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = "abc123";

			// Act
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await ((CloudBlob)null).ReleaseLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);
			});
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
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await ((CloudBlob)null).RenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);
			});
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
		public void UploadStreamAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			// Act
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await ((CloudBlob)null).UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);
			});
		}

		[Fact]
		public void UploadStreamAsync_throws_when_stream_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var stream = (Stream)null;

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict);

			// Act
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await mockBlob.Object.UploadStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);
			});
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
		public void SetMetadataAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;

			// Act
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await ((CloudBlob)null).SetMetadataAsync(leaseId, cancellationToken).ConfigureAwait(false);
			});
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
		public void DownloadTextAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;

			// Act
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await ((CloudBlob)null).DownloadTextAsync(cancellationToken).ConfigureAwait(false);
			});
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
		public void AppendStreamAsync_throws_when_blob_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var streamContent = "Hello World".ToBytes();
			var stream = new MemoryStream(streamContent);

			// Act
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await ((CloudBlob)null).AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);
			});
		}

		[Fact]
		public void AppendStreamAsync_throws_when_stream_is_null()
		{
			// Arrange
			var cancellationToken = CancellationToken.None;
			var leaseId = (string)null;
			var stream = (Stream)null;

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict);

			// Act
			Should.ThrowAsync<ArgumentNullException>(async () =>
			{
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);
			});
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

		// Can't mock GetSharedAccessSignature because it's not marked as 'virtual
		/*
		[Fact]
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

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, FAKE_BLOB_URI);
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

		[Fact]
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

			var mockBlob = new Mock<CloudBlob>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
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
		*/
	}
}
