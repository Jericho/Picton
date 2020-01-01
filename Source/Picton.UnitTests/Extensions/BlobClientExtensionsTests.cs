using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
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
	public class BlobClientExtensionsTests
	{
		private static readonly string BLOB_STORAGE_URL = "http://bogus:10000/devstoreaccount1/";
		private static readonly string BLOB_CONTAINER_NAME = "MyContainer";
		private static readonly string BLOB_ITEM_URL = $"{BLOB_STORAGE_URL}{BLOB_CONTAINER_NAME}/Blob.txt";

		public class TryAcquireLeaseAsync
		{
			[Fact]
			public async Task Throws_when_blob_is_null()
			{
				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).TryAcquireLeaseAsync()).ConfigureAwait(false);
			}

			[Fact]
			public async Task Throws_when_maxLeaseAttempts_is_too_small()
			{
				// Arrange
				var blobClient = new BlobClient(new Uri(BLOB_ITEM_URL));

				// Act
				await Should.ThrowAsync<ArgumentOutOfRangeException>(() => blobClient.TryAcquireLeaseAsync(maxLeaseAttempts: 0)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Throws_when_maxLeaseAttempts_is_too_large()
			{
				// Arrange
				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlob.Object.TryAcquireLeaseAsync(maxLeaseAttempts: 11)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = TimeSpan.FromSeconds(7);
				var expected = "MyLeaseId";

				var mockBlobLease = new Mock<BlobLease>(MockBehavior.Strict);
				mockBlobLease
					.SetupGet(c => c.LeaseId)
					.Returns(expected);

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.Setup(c => c.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions>(), cancellationToken))
					.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))
					.Verifiable();

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				mockBlobClient
					.Setup(c => c.GetBlobLeaseClient(It.IsAny<string>()))
					.Returns(mockLeaseClient.Object)
					.Verifiable();

				// Act
				var result = await mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken).ConfigureAwait(false);

				// Assert
				mockLeaseClient.Verify();
				result.ShouldBe(expected);
			}

			[Fact]
			public async Task Already_leased()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = (TimeSpan?)null;

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.Setup(c => c.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions>(), cancellationToken))
					.ThrowsAsync(new RequestFailedException(409, "There is already a lease present.", "LeaseAlreadyPresent", null))
					.Verifiable();

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				mockBlobClient
					.Setup(c => c.GetBlobLeaseClient(It.IsAny<string>()))
					.Returns(mockLeaseClient.Object)
					.Verifiable();

				// Act
				var result = await mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken).ConfigureAwait(false);

				// Assert
				mockLeaseClient.Verify();
				result.ShouldBeNull();
			}

			[Fact]
			public async Task Throws_when_lease_fails()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = (TimeSpan?)null;

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.Setup(c => c.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions>(), cancellationToken))
					.ThrowsAsync(new Exception("An exception occured"))
					.Verifiable();

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				mockBlobClient
					.Setup(c => c.GetBlobLeaseClient(It.IsAny<string>()))
					.Returns(mockLeaseClient.Object)
					.Verifiable();

				// Act
				await Should.ThrowAsync<Exception>(() => mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Throws_when_exception_other_than_HTTP409()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = (TimeSpan?)null;
				var exception = new Exception("An exception occured");

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.Setup(c => c.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions>(), cancellationToken))
					.ThrowsAsync(new RequestFailedException(999, "Let's simulate some problem", "SimulatedProblem", new Exception("???")))
					.Verifiable();

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				mockBlobClient
					.Setup(c => c.GetBlobLeaseClient(It.IsAny<string>()))
					.Returns(mockLeaseClient.Object)
					.Verifiable();

				// Act
				await Should.ThrowAsync<Exception>(() => mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Retries()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = TimeSpan.FromSeconds(15);
				var maxRetries = 5;

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.Setup(c => c.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions>(), cancellationToken))
					.ThrowsAsync(new RequestFailedException(409, "There is already a lease present.", "LeaseAlreadyPresent", null))
					.Verifiable();

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				mockBlobClient
					.Setup(c => c.GetBlobLeaseClient(It.IsAny<string>()))
					.Returns(mockLeaseClient.Object)
					.Verifiable();

				// Act
				var result = await mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken).ConfigureAwait(false);

				// Assert
				mockLeaseClient.Verify(c => c.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
				result.ShouldBeNull();
			}

			[Fact]
			public async Task Retries_when_lease_is_blank()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = TimeSpan.FromSeconds(15);
				var expected = "";
				var maxRetries = 5;

				var mockBlobLease = new Mock<BlobLease>(MockBehavior.Strict);
				mockBlobLease
					.SetupGet(c => c.LeaseId)
					.Returns(expected);

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.Setup(c => c.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions>(), cancellationToken))
					.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))
					.Verifiable();

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				mockBlobClient
					.Setup(c => c.GetBlobLeaseClient(It.IsAny<string>()))
					.Returns(mockLeaseClient.Object)
					.Verifiable();

				// Act
				var result = await mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken).ConfigureAwait(false);

				// Assert
				mockLeaseClient.Verify(c => c.AcquireAsync(It.IsAny<TimeSpan>(), It.IsAny<RequestConditions>(), It.IsAny<CancellationToken>()), Times.Exactly(maxRetries));
				result.ShouldBe(expected);
			}
		}

		public class AcquireLeaseAsync
		{
			[Fact]
			public async Task Default_lease_time()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = (TimeSpan?)null;
				var expected = "Hello World";

				var mockBlobLease = new Mock<BlobLease>(MockBehavior.Strict);
				mockBlobLease
					.SetupGet(c => c.LeaseId)
					.Returns(expected);

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.Setup(c => c.AcquireAsync(TimeSpan.FromSeconds(15), It.IsAny<RequestConditions>(), cancellationToken))
					.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))
					.Verifiable();

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				mockBlobClient
					.Setup(c => c.GetBlobLeaseClient(It.IsAny<string>()))
					.Returns(mockLeaseClient.Object)
					.Verifiable();

				// Act
				var result = await mockBlobClient.Object.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);

				// Assert
				mockLeaseClient.Verify();
				result.ShouldBe(expected);
			}

			[Fact]
			public async Task Throws_when_leasetime_too_short()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = TimeSpan.FromSeconds(10);

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

				// Act
				await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlobClient.Object.AcquireLeaseAsync(leaseTime, cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Thirty_seconds()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = TimeSpan.FromSeconds(30);
				var expected = "Hello World";

				var mockBlobLease = new Mock<BlobLease>(MockBehavior.Strict);
				mockBlobLease
					.SetupGet(c => c.LeaseId)
					.Returns(expected);

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.Setup(c => c.AcquireAsync(leaseTime, It.IsAny<RequestConditions>(), cancellationToken))
					.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))
					.Verifiable();

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));
				mockBlobClient
					.Setup(c => c.GetBlobLeaseClient(It.IsAny<string>()))
					.Returns(mockLeaseClient.Object)
					.Verifiable();

				// Act
				var result = await mockBlobClient.Object.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);

				// Assert
				mockLeaseClient.Verify();
				result.ShouldBe(expected);
			}

			[Fact]
			public async Task Throws_when_leasetime_too_long()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = TimeSpan.FromMinutes(2);

				var mockBlobClient = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL));

				// Act
				await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlobClient.Object.AcquireLeaseAsync(leaseTime, cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Creates_blob_if_does_not_exist()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = TimeSpan.FromSeconds(30);
				var expected = "xxxxxxxxx";

				var mockBlobLease = new Mock<BlobLease>(MockBehavior.Strict);
				mockBlobLease
					.SetupGet(c => c.LeaseId)
					.Returns(expected);

				var mockLeaseClient = new Mock<BlobLeaseClient>(MockBehavior.Strict);
				mockLeaseClient
					.SetupSequence(c => c.AcquireAsync(leaseTime, It.IsAny<RequestConditions>(), cancellationToken))
						.ThrowsAsync(new RequestFailedException(404, "NotFound", "BlobNotFound", new Exception("???")))
						.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")));

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.UploadAsync(It.IsAny<Stream>(), It.IsAny<BlobHttpHeaders>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<BlobRequestConditions>(), It.IsAny<IProgress<long>>(), It.IsAny<AccessTier>(), It.IsAny<StorageTransferOptions>(), cancellationToken))
					.ReturnsAsync(Response.FromValue(new BlobContentInfo()))
					.Verifiable();

				// Act
				var result = await mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
				result.ShouldBe(expected);
			}
		}

		public class ReleaseLeaseAsync
		{
			[Fact]
			public void ReleaseLeaseAsync_throws_when_blob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";

				// Act
				Should.Throw<ArgumentNullException>(() => ((BlobClient)null).ReleaseLeaseAsync(leaseId, cancellationToken));
			}

			//[Fact]
			//public async Task ReleaseLeaseAsync()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseId = "abc123";

			//	var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlob
			//		.Setup(c => c.ReleaseLeaseAsync(It.IsAny<AccessCondition>(), null, null, cancellationToken))
			//		.Returns(Task.FromResult(true))
			//		.Verifiable();

			//	// Act
			//	await mockBlob.Object.ReleaseLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

			//	// Assert
			//	mockBlob.Verify();
			//}
		}

		public class RenewLeaseAsync
		{
			[Fact]
			public void Throws_when_blob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";

				// Act
				Should.Throw<ArgumentNullException>(() => ((BlobClient)null).RenewLeaseAsync(leaseId, cancellationToken));
			}

			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.RenewLeaseAsync(leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.RenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}
		}

		public class TryRenewLeaseAsync
		{
			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.RenewLeaseAsync(leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				var result = await mockBlob.Object.TryRenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
				result.ShouldBeTrue();
			}

			[Fact]
			public async Task Failure()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.RenewLeaseAsync(leaseId, cancellationToken))
					.Throws(new Exception("Something went wrong"))
					.Verifiable();

				// Act
				var result = await mockBlob.Object.TryRenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
				result.ShouldBeFalse();
			}
		}

		public class UploadStreamAsync
		{
			[Fact]
			public async Task Throws_when_blob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).UploadStreamAsync(stream, null, null, null, leaseId, cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Throws_when_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => mockBlob.Object.UploadStreamAsync(stream, null, null, null, leaseId, cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<BlockBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.UploadStreamAsync(stream, null, null, null, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task With_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<BlockBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.UploadStreamAsync(stream, null, null, null, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_AppendBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<AppendBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				//mockBlob
				//	.Setup(c => c.cre.CreateOrReplaceAsync(null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				//	.Returns(Task.FromResult(true))
				//	.Verifiable();
				//mockBlob
				//	.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), null, It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				//	.Returns(Task.FromResult(true))
				//	.Verifiable();

				// Act
				await mockBlob.Object.UploadStreamAsync(stream, null, null, null, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_AppendBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<AppendBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				//mockBlob
				//	.Setup(c => c.CreateOrReplaceAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				//	.Returns(Task.FromResult(true))
				//	.Verifiable();
				//mockBlob
				//	.Setup(c => c.AppendFromStreamAsync(It.IsAny<Stream>(), It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				//	.Returns(Task.FromResult(true))
				//	.Verifiable();

				// Act
				await mockBlob.Object.UploadStreamAsync(stream, null, null, null, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_PageBlobClient_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<PageBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.UploadStreamAsync(stream, null, null, null, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task Throws_when_unknown_blob_type()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<BlobBaseClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<Exception>(async () => await mockBlob.Object.UploadStreamAsync(stream, null, null, null, leaseId, cancellationToken).ConfigureAwait(false), "Unknow blob type: BlobBaseClientProxy").ConfigureAwait(false);
			}
		}

		public class DownloadTextAsync
		{
			[Fact]
			public async Task Throws_when_blob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).DownloadTextAsync(cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var expected = "Hello World";

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				//mockBlob
				//	.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), null, null, null, cancellationToken))
				//	.Returns(Task.FromResult(true))
				//	.Callback(async (Stream s, AccessCondition ac, BlobRequestOptions o, OperationContext oc, CancellationToken ct) =>
				//	{
				//		var buffer = expected.ToBytes();
				//		await s.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
				//	})
				//	.Verifiable();

				// Act
				var result = await mockBlob.Object.DownloadTextAsync(cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
				result.ShouldBe(expected);
			}
		}

		public class UploadTextAsync
		{
			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var content = "Hello World";

				var mockBlob = new Mock<BlockBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, null, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.UploadTextAsync(content, null, null, null, null, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}
		}

		public class AppendStreamAsync
		{
			[Fact]
			public async Task Throws_when_blob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).AppendStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Throws_when_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<BlockBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.Returns(Task.FromResult(true))
					.Verifiable();
				mockBlob
					.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(), cancellationToken))
					.ReturnsAsync(new MockAzureResponse(200, "ok"))
					.Verifiable();
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, null, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task With_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<BlockBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.Returns(Task.FromResult(true))
					.Verifiable();
				mockBlob
					.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(), It.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), It.IsAny<StorageTransferOptions>(), cancellationToken))
					.ReturnsAsync(new MockAzureResponse(200, "ok"))
					.Verifiable();
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task Blob_does_not_exist()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<BlockBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(false)
					.Verifiable();
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_AppendBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<AppendBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(true)
					.Verifiable();
				mockBlob
					.Setup(c => c.AppendStreamAsync(It.IsAny<Stream>(), null, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_AppendBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<AppendBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(true)
					.Verifiable();
				mockBlob
					.Setup(c => c.AppendStreamAsync(It.IsAny<Stream>(), leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_AppendBlob_blob_does_not_exist()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<AppendBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(false)
					.Verifiable();
				//mockBlob
				//	.Setup(c => c.CreateOrReplaceAsync(It.Is<AccessCondition>(ac => ac.LeaseId == leaseId), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), cancellationToken))
				//	.Returns(Task.FromResult(true))
				//	.Verifiable();
				mockBlob
					.Setup(c => c.AppendStreamAsync(It.IsAny<Stream>(), leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_PageBlobClient_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<PageBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(true)
					.Verifiable();
				mockBlob
					.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(), null, It.IsAny<StorageTransferOptions>(), cancellationToken))
					.ReturnsAsync(new MockAzureResponse(200, "ok"))
					.Verifiable();
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_PageBlobClient_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<PageBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(true)
					.Verifiable();
				mockBlob
					.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(), It.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), It.IsAny<StorageTransferOptions>(), cancellationToken))
					.ReturnsAsync(new MockAzureResponse(200, "ok"))
					.Verifiable();
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task To_PageBlobClient_blob_does_not_exist()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<PageBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(false)
					.Verifiable();
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}

			[Fact]
			public async Task Throws_when_unknown_blob_type()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(true)
					.Verifiable();

				// Act
				var e = await Should.ThrowAsync<Exception>(() => mockBlob.Object.AppendStreamAsync(stream, leaseId, cancellationToken)).ConfigureAwait(false);

				// Assert
				e.Message.ShouldStartWith("Unknow blob type: ");
			}
		}

		public class AppendBytesAsync
		{
			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var buffer = "Hello World".ToBytes();

				var mockBlob = new Mock<BlockBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(true)
					.Verifiable();
				mockBlob
					.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(), null, It.IsAny<StorageTransferOptions>(), cancellationToken))
					.ReturnsAsync(new MockAzureResponse(200, "ok"))
					.Verifiable();
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendBytesAsync(buffer, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}
		}

		public class AppendTextAsync
		{
			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var content = "Hello World";

				var mockBlob = new Mock<BlockBlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlob
					.Setup(c => c.ExistsAsync(cancellationToken))
					.ReturnsAsync(true)
					.Verifiable();
				mockBlob
					.Setup(c => c.DownloadToAsync(It.IsAny<Stream>(), null, It.IsAny<StorageTransferOptions>(), cancellationToken))
					.ReturnsAsync(new MockAzureResponse(200, "ok"))
					.Verifiable();
				mockBlob
					.Setup(c => c.UploadStreamAsync(It.IsAny<Stream>(), null, null, null, leaseId, cancellationToken))
					.Returns(Task.CompletedTask)
					.Verifiable();

				// Act
				await mockBlob.Object.AppendTextAsync(content, leaseId, cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
			}
		}

		public class DownloadByteArrayAsync
		{
			[Fact]
			public async Task Throws_when_blob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).DownloadByteArrayAsync(cancellationToken)).ConfigureAwait(false);
			}

			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var expected = "Hello World";

				var mockBlob = new Mock<BlobClient>(MockBehavior.Strict, new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				//mockBlob
				//	.Setup(c => c.DownloadToStreamAsync(It.IsAny<Stream>(), null, null, null, cancellationToken))
				//	.Returns(Task.FromResult(true))
				//	.Callback(async (Stream s, AccessCondition ac, BlobRequestOptions bro, OperationContext oc, CancellationToken ct) =>
				//	{
				//		var buffer = expected.ToBytes();
				//		await s.WriteAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
				//	})
				//	.Verifiable();

				// Act
				var result = await mockBlob.Object.DownloadByteArrayAsync(cancellationToken).ConfigureAwait(false);

				// Assert
				mockBlob.Verify();
				result.ShouldBe(expected.ToBytes());
			}
		}

		public class GetSharedAccessSignatureUri
		{
			[Fact]
			public void Throws_when_blob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var credential = new StorageSharedKeyCredential("..account name...", "...key...");
				var permissions = BlobSasPermissions.Read;
				var duration = TimeSpan.FromMinutes(1);
				var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;

				// Act
				Should.Throw<ArgumentNullException>(() =>
				{
					var result = ((BlobClient)null).GetSharedAccessSignatureUri(credential, permissions, duration, systemClock);
				});
			}

			[Fact]
			public void With_specified_duration()
			{
				// Arrange
				var credential = new StorageSharedKeyCredential("..account name...", "...key...");
				var permissions = BlobSasPermissions.Read;
				var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;
				var duration = TimeSpan.FromMinutes(1);

				var blob = new BlobClient(new Uri(BLOB_ITEM_URL));

				// Act
				var result = blob.GetSharedAccessSignatureUri(credential, permissions, duration, systemClock);

				// Assert
				var qs = ParseQueryString(result);
				qs["st"].ShouldBe("2016-08-12T14:55:00Z");
				qs["se"].ShouldBe("2016-08-12T15:01:00Z");
			}

			[Fact]
			public void Default_duration()
			{
				// Arrange
				var credential = new StorageSharedKeyCredential("..account name...", "...key...");
				var permissions = BlobSasPermissions.Read;
				var systemClock = new MockSystemClock(new DateTime(2016, 8, 12, 15, 0, 0, 0, DateTimeKind.Utc)).Object;

				var blob = new BlobClient(new Uri(BLOB_ITEM_URL));

				// Act
				var result = blob.GetSharedAccessSignatureUri(credential, permissions, systemClock: systemClock);

				// Assert
				var qs = ParseQueryString(result);
				qs["st"].ShouldBe("2016-08-12T14:55:00Z");
				qs["se"].ShouldBe("2016-08-12T15:15:00Z");
			}
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
