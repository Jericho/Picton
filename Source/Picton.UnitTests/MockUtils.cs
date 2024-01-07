namespace Picton.UnitTests
{
	internal static class MockUtils
	{
		internal static readonly string QUEUE_STORAGE_URL = "http://bogus:10001/devstoreaccount1/";
		internal static readonly string BLOB_STORAGE_URL = "http://bogus:10002/devstoreaccount1/";

		internal static BlobContainerClient GetMockBlobContainerClient(string containerName = "mycontainer", IEnumerable<BlobClient> mockBlobClients = null)
		{
			var mockContainerUri = new Uri(BLOB_STORAGE_URL + containerName);
			var blobContainerInfo = BlobsModelFactory.BlobContainerInfo(ETag.All, DateTimeOffset.UtcNow);
			var mockBlobContainer = Substitute.For<BlobContainerClient>();

			mockBlobContainer
				.Name
				.Returns(containerName);

			mockBlobContainer
				.Uri
				.Returns(mockContainerUri);

			mockBlobContainer
				.CreateIfNotExists(Arg.Any<PublicAccessType>(), Arg.Any<IDictionary<string, string>>(), Arg.Any<BlobContainerEncryptionScopeOptions>(), Arg.Any<CancellationToken>())
				.Returns(Response.FromValue(blobContainerInfo, new MockAzureResponse(200, "ok")));

			foreach (var blobClient in mockBlobClients ?? Enumerable.Empty<BlobClient>())
			{
				mockBlobContainer
					.GetBlobClient(blobClient.Name)
					.Returns(blobClient);
			}

			return mockBlobContainer;
		}

		internal static BlobClient GetMockBlobClient(string blobName)
		{
			var mockBlobUri = new Uri(BLOB_STORAGE_URL + blobName);
			var mockBlobClient = Substitute.For<BlobClient>();

			mockBlobClient
				.Name
				.Returns(blobName);

			mockBlobClient
				.Uri
				.Returns(mockBlobUri);

			return mockBlobClient;
		}

		internal static QueueClient GetMockQueueClient(string queueName = "myqueue")
		{
			var mockQueueStorageUri = new Uri(QUEUE_STORAGE_URL + queueName);
			var mockQueueClient = Substitute.For<QueueClient>();

			mockQueueClient
				.Uri
				.Returns(mockQueueStorageUri);

			mockQueueClient
				.MessageMaxBytes
				.Returns(65536);

			mockQueueClient
				.MaxPeekableMessages
				.Returns(32);

			mockQueueClient
				.CreateIfNotExists(Arg.Any<IDictionary<string, string>>(), Arg.Any<CancellationToken>())
				.Returns((Response)null);

			return mockQueueClient;
		}
	}
}
