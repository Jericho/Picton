using Azure;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using NSubstitute;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).TryAcquireLeaseAsync());
			}

			[Fact]
			public async Task Throws_when_maxAttempts_is_too_small()
			{
				// Arrange
				var blobClient = new BlobClient(new Uri(BLOB_ITEM_URL));

				// Act
				await Should.ThrowAsync<ArgumentOutOfRangeException>(() => blobClient.TryAcquireLeaseAsync(maxAttempts: 0));
			}

			[Fact]
			public async Task Throws_when_maxAttempts_is_too_large()
			{
				// Arrange
				var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlobClient.TryAcquireLeaseAsync(maxAttempts: 11));
			}

			//[Fact]
			//public async Task Success()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = TimeSpan.FromSeconds(7);
			//	var expected = "MyLeaseId";

			//	var mockBlobLease = Substitute.For<BlobLease>();
			//	mockBlobLease
			//		.LeaseId)
			//		.Returns(expected);

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	var result = await mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken);

			//	// Assert
			//	result.ShouldBe(expected);
			//}

			//[Fact]
			//public async Task Already_leased()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = (TimeSpan?)null;

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ThrowsAsync(new RequestFailedException(409, "There is already a lease present.", "LeaseAlreadyPresent", null))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	var result = await mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken);

			//	// Assert
			//	result.ShouldBeNull();
			//}

			//[Fact]
			//public async Task Throws_when_lease_fails()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = (TimeSpan?)null;

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ThrowsAsync(new Exception("An exception occured"))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	await Should.ThrowAsync<Exception>(() => mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken));
			//}

			//[Fact]
			//public async Task Throws_when_exception_other_than_HTTP409()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = (TimeSpan?)null;
			//	var exception = new Exception("An exception occured");

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ThrowsAsync(new RequestFailedException(999, "Let's simulate some problem", "SimulatedProblem", new Exception("???")))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	await Should.ThrowAsync<Exception>(() => mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, 1, cancellationToken));
			//}

			//[Fact]
			//public async Task Retries()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = TimeSpan.FromSeconds(15);
			//	var maxRetries = 5;

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ThrowsAsync(new RequestFailedException(409, "There is already a lease present.", "LeaseAlreadyPresent", null))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	var result = await mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken);

			//	// Assert
			//	mockLeaseClient.Verify(c => c.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), Arg.Any<CancellationToken>()), Times.Exactly(maxRetries));
			//	result.ShouldBeNull();
			//}

			//[Fact]
			//public async Task Retries_when_lease_is_blank()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = TimeSpan.FromSeconds(15);
			//	var expected = "";
			//	var maxRetries = 5;

			//	var mockBlobLease = Substitute.For<BlobLease>();
			//	mockBlobLease
			//		.LeaseId)
			//		.Returns(expected);

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	var result = await mockBlobClient.Object.TryAcquireLeaseAsync(leaseTime, maxRetries, cancellationToken);

			//	// Assert
			//	mockLeaseClient.Verify(c => c.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), Arg.Any<CancellationToken>()), Times.Exactly(maxRetries));
			//	result.ShouldBe(expected);
			//}
		}

		public class AcquireLeaseAsync
		{
			//[Fact]
			//public async Task Default_lease_time()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = (TimeSpan?)null;
			//	var expected = "Hello World";

			//	var mockBlobLease = Substitute.For<BlobLease>();
			//	mockBlobLease
			//		.LeaseId)
			//		.Returns(expected);

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(TimeSpan.FromSeconds(15), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	var result = await mockBlobClient.Object.AcquireLeaseAsync(leaseTime, cancellationToken);

			//	// Assert
			//	result.ShouldBe(expected);
			//}

			//[Fact]
			//public async Task Throws_when_leasetime_too_short()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = TimeSpan.FromSeconds(10);

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

			//	// Act
			//	await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlobClient.Object.AcquireLeaseAsync(leaseTime, cancellationToken));
			//}

			//[Fact]
			//public async Task Thirty_seconds()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = TimeSpan.FromSeconds(30);
			//	var expected = "Hello World";

			//	var mockBlobLease = Substitute.For<BlobLease>();
			//	mockBlobLease
			//		.LeaseId)
			//		.Returns(expected);

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(leaseTime, Arg.Any<RequestConditions>(), cancellationToken))
			//		.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	var result = await mockBlobClient.Object.AcquireLeaseAsync(leaseTime, cancellationToken);

			//	// Assert
			//	result.ShouldBe(expected);
			//}

			[Fact]
			public async Task Throws_when_leasetime_too_long()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseTime = TimeSpan.FromMinutes(2);

				var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentOutOfRangeException>(() => mockBlobClient.AcquireLeaseAsync(leaseTime, cancellationToken));
			}

			//[Fact]
			//public async Task Creates_blob_if_does_not_exist()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseTime = TimeSpan.FromSeconds(30);
			//	var expected = "xxxxxxxxx";

			//	var mockBlobLease = Substitute.For<BlobLease>();
			//	mockBlobLease
			//		.LeaseId)
			//		.Returns(expected);

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.SetupSequence(c => c.AcquireAsync(leaseTime, Arg.Any<RequestConditions>(), cancellationToken))
			//			.ThrowsAsync(new RequestFailedException(404, "NotFound", "BlobNotFound", new Exception("???")))
			//			.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")));

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobHttpHeaders>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<BlobRequestConditions>(), Arg.Any<IProgress<long>>(), Arg.Any<AccessTier>(), Arg.Any<StorageTransferOptions>(), cancellationToken))
			//		.ReturnsAsync(Response.FromValue(new BlobContentInfo()))

			//	// Act
			//	var result = await mockBlob.Object.AcquireLeaseAsync(leaseTime, cancellationToken);

			//	// Assert
			//	result.ShouldBe(expected);
			//}
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

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.ReleaseLeaseAsync(Arg.Any<AccessCondition>(), null, null, cancellationToken))
			//		.Returns(true);

			//	// Act
			//	await mockBlob.Object.ReleaseLeaseAsync(leaseId, cancellationToken);

			//	// Assert
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

			//[Fact]
			//public async Task Success()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseId = "abc123";

			//	var mockBlobLease = Substitute.For<BlobLease>();
			//	mockBlobLease
			//		.LeaseId)
			//		.Returns(leaseId);

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	await mockBlobClient.Object.RenewLeaseAsync(leaseId, cancellationToken);

			//	// Assert
			//}
		}

		public class TryRenewLeaseAsync
		{
			//[Fact]
			//public async Task Success()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseId = "abc123";

			//	var mockBlobLease = Substitute.For<BlobLease>();
			//	mockBlobLease
			//		.LeaseId)
			//		.Returns(leaseId);

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.ReturnsAsync(Response.FromValue(mockBlobLease.Object, new MockAzureResponse(200, "ok")))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	var result = await mockBlobClient.Object.TryRenewLeaseAsync(leaseId, cancellationToken);

			//	// Assert
			//	result.ShouldBeTrue();
			//}

			//[Fact]
			//public async Task Failure()
			//{
			//	// Arrange
			//	var cancellationToken = CancellationToken.None;
			//	var leaseId = "abc123";

			//	var mockLeaseClient = Substitute.For<BlobLeaseClient>();
			//	mockLeaseClient
			//		.AcquireAsync(Arg.Any<TimeSpan>(), Arg.Any<RequestConditions>(), cancellationToken))
			//		.Throws(new Exception("Something went wrong"))

			//	var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
			//	mockBlobClient
			//		.GetBlobLeaseClient(Arg.Any<string>()))
			//		.Returns(mockLeaseClient.Object)

			//	// Act
			//	var result = await mockBlobClient.Object.TryRenewLeaseAsync(leaseId, cancellationToken);

			//	// Assert
			//	result.ShouldBeFalse();
			//}
		}

		public class UploadStreamAsync
		{
			[Fact]
			public async Task Throws_when_PageBlob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var stream = new MemoryStream(newContent.ToBytes());

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((PageBlobClient)null).UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken));
			}

			[Fact]
			public async Task Throws_when_PageBlob_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var mockBlobClient = Substitute.For<PageBlobClient>();

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken));
			}

			[Fact]
			public async Task Create_new_PageBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1, null);

				var mockBlobClient = Substitute.For<PageBlobClient>();
				mockBlobClient
					.When(callInfo => callInfo.GetPageRangesAsync(null, null, null, cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.CreateAsync(5 * 1024 * 1024, null, Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadPagesAsync(Arg.Any<Stream>(), 0, null, null, null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.PageBlobPageBytes
					.Returns(512);
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_PageBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1, null);

				var mockBlobClient = Substitute.For<PageBlobClient>();
				mockBlobClient
					.When(callInfo => callInfo.GetPageRangesAsync(null, null, Arg.Is<PageBlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.CreateAsync(5 * 1024 * 1024, null, Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadPagesAsync(Arg.Any<Stream>(), 0, null, null, null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.PageBlobPageBytes
					.Returns(512);
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Overwrite_existing_PageBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var pageRangesInfo = BlobsModelFactory.PageRangesInfo(DateTimeOffset.UtcNow, ETag.All, 222, new[] { new HttpRange(0, 221) }, new[] { new HttpRange(0, 221) });
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1, null);

				var mockBlobClient = Substitute.For<PageBlobClient>();
				mockBlobClient
					.GetPageRangesAsync(null, null, null, cancellationToken)
					.Returns(Response.FromValue(pageRangesInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.ClearPagesAsync(Arg.Is<HttpRange>(r => r.Offset == 0 && r.Length == pageRangesInfo.BlobContentLength), null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadPagesAsync(Arg.Any<Stream>(), 0, null, null, null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.PageBlobPageBytes
					.Returns(512);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Overwrite_existing_PageBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var pageRangesInfo = BlobsModelFactory.PageRangesInfo(DateTimeOffset.UtcNow, ETag.All, 222, new[] { new HttpRange(0, 221) }, new[] { new HttpRange(0, 221) });
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1, null);

				var mockBlobClient = Substitute.For<PageBlobClient>();
				mockBlobClient
					.GetPageRangesAsync(null, null, Arg.Is<PageBlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken)
					.Returns(Response.FromValue(pageRangesInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.ClearPagesAsync(Arg.Is<HttpRange>(r => r.Offset == 0 && r.Length == pageRangesInfo.BlobContentLength), Arg.Is<PageBlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadPagesAsync(Arg.Any<Stream>(), 0, null, Arg.Is<PageBlobRequestConditions>(rc => rc.LeaseId == leaseId), null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.PageBlobPageBytes
					.Returns(512);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Throws_when_BlockBlob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlockBlobClient)null).UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken));
			}

			[Fact]
			public async Task Throws_when_BlockBlob_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var blobClient = new BlockBlobClient(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => blobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken));
			}

			[Fact]
			public async Task Create_new_BlockBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);

				var mockBlobClient = Substitute.For<BlockBlobClient>();
				mockBlobClient
					.When(callInfo => callInfo.GetPropertiesAsync(null, cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_BlockBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);

				var mockBlobClient = Substitute.For<BlockBlobClient>();
				mockBlobClient
					.When(callInfo => callInfo.GetPropertiesAsync(Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Overwrite_existing_BlockBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var existingContent = "Hello World";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, existingContent.ToMD5Hash(), null, 1);

				var mockBlobClient = Substitute.For<BlockBlobClient>();
				mockBlobClient
					.GetPropertiesAsync(null, cancellationToken)
					.Returns(Response.FromValue(new BlobProperties(), new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Overwrite_existing_BlockBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var existingContent = "Hello World";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, existingContent.ToMD5Hash(), null, 1);

				var mockBlobClient = Substitute.For<BlockBlobClient>();
				mockBlobClient
					.GetPropertiesAsync(Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken)
					.Returns(Response.FromValue(new BlobProperties(), new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), null, null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Throws_when_AppendBlob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((AppendBlobClient)null).UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken));
			}

			[Fact]
			public async Task Throws_when_AppendBlob_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var blobClient = new AppendBlobClient(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => blobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken));
			}

			[Fact]
			public async Task Create_new_AppendBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);
				var blobAppendInfo = BlobsModelFactory.BlobAppendInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, null, 1, false, null);

				var mockBlobClient = Substitute.For<AppendBlobClient>();
				mockBlobClient
					.AppendBlockAsync(Arg.Any<Stream>(), null, null, null, cancellationToken)
					.Returns(
						callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")),
						callInfo => Response.FromValue(blobAppendInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.CreateAsync(Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_AppendBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);
				var blobAppendInfo = BlobsModelFactory.BlobAppendInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, null, 1, false, null);

				var mockBlobClient = Substitute.For<AppendBlobClient>();
				mockBlobClient
					.When(callInfo => callInfo.AppendBlockAsync(Arg.Any<Stream>(), null, Arg.Is<AppendBlobRequestConditions>(rc => rc.LeaseId == leaseId), null, cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient.CreateAsync(Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.AppendBlockAsync(Arg.Any<Stream>(), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobAppendInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Overwrite_existing_AppenBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var stream = new MemoryStream(newContent.ToBytes());

				var blobAppendInfo = BlobsModelFactory.BlobAppendInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, null, 1, false, null);

				var mockBlobClient = Substitute.For<AppendBlobClient>();
				mockBlobClient
					.AppendBlockAsync(Arg.Any<Stream>(), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobAppendInfo, new MockAzureResponse(200, "ok")));

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Overwrite_existing_AppenBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobAppendInfo = BlobsModelFactory.BlobAppendInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, null, 1, false, null);

				var mockBlobClient = Substitute.For<AppendBlobClient>();
				mockBlobClient
					.AppendBlockAsync(Arg.Any<Stream>(), null, Arg.Is<AppendBlobRequestConditions>(rc => rc.LeaseId == leaseId), null, cancellationToken)
					.Returns(Response.FromValue(blobAppendInfo, new MockAzureResponse(200, "ok")));

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Throws_when_BlobClient_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken));
			}

			[Fact]
			public async Task Throws_when_BlobClient_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var blobClient = new BlobClient(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => blobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken));
			}

			[Fact]
			public async Task Create_new_BlobClient_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.When(callInfo => callInfo.GetPropertiesAsync(null, cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_BlobClient_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.When(callInfo => callInfo.GetPropertiesAsync(Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Overwrite_existing_BlobClient_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.GetPropertiesAsync(null, cancellationToken)
					.Returns(Response.FromValue(new BlobProperties(), new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Overwrite_existing_BlobClient_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.GetPropertiesAsync(Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken)
					.Returns(Response.FromValue(new BlobProperties(), new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Throws_when_unknown_blob_type()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var stream = new MemoryStream(newContent.ToBytes());

				var mockBlob = Substitute.For<BlobBaseClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<Exception>(async () => await mockBlob.UploadStreamAsync(stream, leaseId, null, null, null, cancellationToken), "Unknow blob type: BlobBaseClientProxy");
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
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).DownloadTextAsync(cancellationToken));
			}

			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var expected = "Hello World";
				var content = new MemoryStream(expected.ToBytes());
				var blobUri = new Uri(BLOB_ITEM_URL);

				var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: content);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.DownloadAsync(cancellationToken)
					.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));

				// Act
				var result = await mockBlobClient.DownloadTextAsync(cancellationToken);

				// Assert
				result.ShouldBe(expected);
			}
		}

		public class UploadTextAsync
		{
			[Fact]
			public async Task Success_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "Hello World";
				var blobUri = new Uri(BLOB_ITEM_URL);

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1, null);

				var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlobClient
					.GetPropertiesAsync(Arg.Any<BlobRequestConditions>(), cancellationToken)
					.Returns(Response.FromValue(new BlobProperties(), new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobHttpHeaders>(), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadTextAsync(newContent, leaseId, null, null, null, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Success_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var newContent = "Hello World";
				var blobUri = new Uri(BLOB_ITEM_URL);

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1, null);

				var mockBlobClient = Substitute.For<BlobClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);
				mockBlobClient
					.GetPropertiesAsync(Arg.Any<BlobRequestConditions>(), cancellationToken)
					.Returns(Response.FromValue(new BlobProperties(), new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Any<BlobHttpHeaders>(), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.UploadTextAsync(newContent, null, null, null, null, cancellationToken);

				// Assert
			}
		}

		public class AppendStreamAsync
		{
			[Fact]
			public async Task Throws_when_PageBlob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var stream = new MemoryStream(newContent.ToBytes());

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((PageBlobClient)null).AppendStreamAsync(stream, leaseId, cancellationToken));
			}

			[Fact]
			public async Task Throws_when_PageBlob_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var mockBlobClient = Substitute.For<BlobClient>();

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken));
			}

			[Fact]
			public async Task Create_new_PageBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1, null);

				var mockBlobClient = Substitute.For<PageBlobClient>();
				mockBlobClient
					.When(callInfo => callInfo.GetPageRangesAsync(null, null, null, cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.CreateAsync(5 * 1024 * 1024, null, Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadPagesAsync(Arg.Any<Stream>(), 0, null, null, null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.PageBlobPageBytes
					.Returns(512);
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_PageBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1, null);

				var mockBlobClient = Substitute.For<PageBlobClient>();
				mockBlobClient
					.When(callInfo => callInfo.GetPageRangesAsync(null, null, Arg.Is<PageBlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.CreateAsync(5 * 1024 * 1024, null, Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadPagesAsync(Arg.Any<Stream>(), 0, null, null, null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.PageBlobPageBytes
					.Returns(512);
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Append_to_existing_PageBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var stream = new MemoryStream(newContent.ToBytes());

				var pageRangesInfo = BlobsModelFactory.PageRangesInfo(DateTimeOffset.UtcNow, ETag.All, 222, new[] { new HttpRange(0, 221) }, new[] { new HttpRange(0, 221) });
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1, null);

				var mockBlobClient = Substitute.For<PageBlobClient>();
				mockBlobClient
					.GetPageRangesAsync(null, null, null, cancellationToken)
					.Returns(Response.FromValue(pageRangesInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadPagesAsync(Arg.Any<Stream>(), pageRangesInfo.BlobContentLength - 1, null, null, null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.PageBlobPageBytes
					.Returns(512);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Append_to_existing_PageBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var pageRangesInfo = BlobsModelFactory.PageRangesInfo(DateTimeOffset.UtcNow, ETag.All, 222, new[] { new HttpRange(0, 221) }, new[] { new HttpRange(0, 221) });
				var pageInfo = BlobsModelFactory.PageInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1, null);

				var mockBlobClient = Substitute.For<PageBlobClient>();
				mockBlobClient
					.GetPageRangesAsync(null, null, Arg.Is<PageBlobRequestConditions>(rc => rc.LeaseId == leaseId), cancellationToken)
					.Returns(Response.FromValue(pageRangesInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadPagesAsync(Arg.Any<Stream>(), pageRangesInfo.BlobContentLength - 1, null, Arg.Is<PageBlobRequestConditions>(rc => rc.LeaseId == leaseId), null, cancellationToken)
					.Returns(Response.FromValue(pageInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.PageBlobPageBytes
					.Returns(512);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Throws_when_BlockBlob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlockBlobClient)null).AppendStreamAsync(stream, leaseId, cancellationToken));
			}

			[Fact]
			public async Task Throws_when_BlockBlob_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var blobClient = new BlockBlobClient(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => blobClient.AppendStreamAsync(stream, leaseId, cancellationToken));
			}

			[Fact]
			public async Task Create_new_BlockBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);

				var mockBlobClient = Substitute.For<BlockBlobClient>();
				mockBlobClient
					.DownloadAsync(default, null, false, cancellationToken)
					.Returns(Task.FromException<Response<BlobDownloadInfo>>(new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???"))));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_BlockBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);

				var mockBlobClient = Substitute.For<BlockBlobClient>();
				mockBlobClient
					.When(callInfo => callInfo.DownloadAsync(default, Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), false, cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Append_to_existing_BlockBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var currentContent = "Hello World";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(currentContent.ToBytes()));
				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);

				var mockBlobClient = Substitute.For<BlockBlobClient>();
				mockBlobClient
					.DownloadAsync(default, null, false, cancellationToken)
					.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Append_to_existing_BlockBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var currentContent = "Hello World";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(currentContent.ToBytes()));
				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, 1);

				var mockBlobClient = Substitute.For<BlockBlobClient>();
				mockBlobClient
					.DownloadAsync(default, Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), false, cancellationToken)
					.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), null, null, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Throws_when_AppendBlob_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((AppendBlobClient)null).AppendStreamAsync(stream, leaseId, cancellationToken));
			}

			[Fact]
			public async Task Throws_when_AppendBlob_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var blobClient = new AppendBlobClient(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => blobClient.AppendStreamAsync(stream, leaseId, cancellationToken));
			}

			[Fact]
			public async Task Create_new_or_append_to_existing_AppendBlob_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);
				var blobAppendInfo = BlobsModelFactory.BlobAppendInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, null, 1, false, null);

				var mockBlobClient = Substitute.For<AppendBlobClient>();
				mockBlobClient
					.CreateIfNotExistsAsync(Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.AppendBlockAsync(Arg.Any<Stream>(), null, null, null, cancellationToken)
					.Returns(Response.FromValue(blobAppendInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_or_append_to_existing_AppendBlob_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);
				var blobAppendInfo = BlobsModelFactory.BlobAppendInfo(ETag.All, DateTimeOffset.UtcNow, newContent.ToMD5Hash(), null, null, 1, false, null);

				var mockBlobClient = Substitute.For<AppendBlobClient>();
				mockBlobClient
					.CreateIfNotExistsAsync(Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.AppendBlockAsync(Arg.Any<Stream>(), null, Arg.Is<AppendBlobRequestConditions>(rc => rc.LeaseId == leaseId), null, cancellationToken)
					.Returns(Response.FromValue(blobAppendInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Throws_when_BlobClient_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var streamContent = "Hello World".ToBytes();
				var stream = new MemoryStream(streamContent);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobClient)null).AppendStreamAsync(stream, leaseId, cancellationToken));
			}

			[Fact]
			public async Task Throws_when_BlobClient_stream_is_null()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var stream = (Stream)null;

				var blobClient = new BlobClient(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<ArgumentNullException>(() => blobClient.AppendStreamAsync(stream, leaseId, cancellationToken));
			}

			[Fact]
			public async Task Append_to_existing_BlobClient_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var currentContent = "Hello World";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(currentContent.ToBytes()));
				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.DownloadAsync(default, null, false, cancellationToken)
					.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Append_to_existing_BlobClient_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var currentContent = "Hello World";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(currentContent.ToBytes()));
				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.DownloadAsync(default, Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), false, cancellationToken)
					.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_or_append_to_existing_BlobClient_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.When(callInfo => callInfo.DownloadAsync(default, null, false, cancellationToken))
					.Do(callInfo => throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Create_new_or_append_to_existing_BlobClient_with_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.When(callInfo => callInfo.DownloadAsync(default, Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), false, cancellationToken))
					.Do(callInfo => { throw new RequestFailedException(404, "Blob not found", "BlobNotFound", new Exception("???")); });
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), Arg.Is<BlobRequestConditions>(rc => rc.LeaseId == leaseId), null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendStreamAsync(stream, leaseId, cancellationToken);

				// Assert
			}

			[Fact]
			public async Task Throws_when_unknown_blob_type()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = "abc123";
				var newContent = "xyz123";
				var stream = new MemoryStream(newContent.ToBytes());

				var mockBlob = Substitute.For<BlobBaseClient>(new Uri(BLOB_ITEM_URL), (BlobClientOptions)null);

				// Act
				await Should.ThrowAsync<Exception>(async () => await mockBlob.AppendStreamAsync(stream, leaseId, cancellationToken), "Unknow blob type: BlobBaseClientProxy");
			}
		}

		public class AppendBytesAsync
		{
			[Fact]
			public async Task Append_to_existing_BlobClient_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var currentContent = "Hello World";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(currentContent.ToBytes()));
				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.DownloadAsync(default, null, false, cancellationToken)
					.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendBytesAsync(newContent.ToBytes(), leaseId, cancellationToken);

				// Assert
			}
		}

		public class AppendTextAsync
		{
			[Fact]
			public async Task Append_to_existing_BlobClient_without_lease()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var leaseId = (string)null;
				var currentContent = "Hello World";
				var newContent = "xyz123";
				var blobUri = new Uri(BLOB_ITEM_URL);
				var stream = new MemoryStream(newContent.ToBytes());

				var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: new MemoryStream(currentContent.ToBytes()));
				var blobContentInfo = BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, null, null, 1);

				var mockBlobClient = Substitute.For<BlobClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.DownloadAsync(default, null, false, cancellationToken)
					.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.UploadAsync(Arg.Any<Stream>(), Arg.Is<BlobHttpHeaders>(headers => headers.ContentType == "text/plain"), Arg.Is<IDictionary<string, string>>(metadata => metadata.Count == 0), null, null, null, default, cancellationToken)
					.Returns(Response.FromValue(blobContentInfo, new MockAzureResponse(200, "ok")));
				mockBlobClient
					.Uri
					.Returns(blobUri);

				// Act
				await mockBlobClient.AppendTextAsync(newContent, leaseId, cancellationToken);

				// Assert
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
				await Should.ThrowAsync<ArgumentNullException>(() => ((BlobBaseClient)null).DownloadByteArrayAsync(cancellationToken));
			}

			[Fact]
			public async Task Success()
			{
				// Arrange
				var cancellationToken = CancellationToken.None;
				var expected = "Hello World";
				var content = new MemoryStream(expected.ToBytes());
				var blobUri = new Uri(BLOB_ITEM_URL);

				var blobDownloadInfo = BlobsModelFactory.BlobDownloadInfo(content: content);

				var mockBlobClient = Substitute.For<BlobBaseClient>(blobUri, (BlobClientOptions)null);
				mockBlobClient
					.DownloadAsync(cancellationToken)
					.Returns(Response.FromValue(blobDownloadInfo, new MockAzureResponse(200, "ok")));

				// Act
				var result = await mockBlobClient.DownloadByteArrayAsync(cancellationToken);

				// Assert
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
				var credential = new StorageSharedKeyCredential("..account name...", Convert.ToBase64String(Encoding.UTF8.GetBytes("xyz123")));
				var permissions = BlobSasPermissions.Read;
				var duration = TimeSpan.FromMinutes(1);
				var systemClock = new MockSystemClock(2016, 8, 12, 15, 0, 0, 0);

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
				var credential = new StorageSharedKeyCredential("..account name...", Convert.ToBase64String(Encoding.UTF8.GetBytes("xyz123")));
				var permissions = BlobSasPermissions.Read;
				var systemClock = new MockSystemClock(2016, 8, 12, 15, 0, 0, 0);
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
				var credential = new StorageSharedKeyCredential("..account name...", Convert.ToBase64String(Encoding.UTF8.GetBytes("xyz123")));
				var permissions = BlobSasPermissions.Read;
				var systemClock = new MockSystemClock(2016, 8, 12, 15, 0, 0, 0);

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
