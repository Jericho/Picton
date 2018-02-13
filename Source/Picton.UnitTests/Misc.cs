using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;
using Moq;
using System;
using System.Text;
using System.Threading;

namespace Picton.UnitTests
{
	internal static class Misc
	{
		public static readonly string BLOB_STORAGE_URL = "http://bogus:10000/devstoreaccount1/";
		public static readonly string QUEUE_STORAGE_URL = "http://bogus:10001/devstoreaccount1/";

		public static Mock<CloudBlobContainer> GetMockBlobContainer(string containerName = "mycontainer")
		{
			var mockContainerUri = new Uri(BLOB_STORAGE_URL + containerName);
			var mockBlobContainer = new Mock<CloudBlobContainer>(MockBehavior.Strict, mockContainerUri);
			mockBlobContainer
				.Setup(c => c.CreateIfNotExistsAsync(It.IsAny<BlobContainerPublicAccessType>(), It.IsAny<BlobRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(true)
				.Verifiable();
			return mockBlobContainer;
		}

		public static Mock<CloudBlobClient> GetMockBlobClient(Mock<CloudBlobContainer> mockBlobContainer)
		{
			var mockBlobStorageUri = new Uri(BLOB_STORAGE_URL);
			var mockBlobClient = new Mock<CloudBlobClient>(MockBehavior.Strict, mockBlobStorageUri);
			mockBlobClient
				.Setup(c => c.GetContainerReference(It.IsAny<string>()))
				.Returns(mockBlobContainer.Object)
				.Verifiable();
			return mockBlobClient;
		}

		public static Mock<CloudQueueClient> GetMockQueueClient(Mock<CloudQueue> mockQueue)
		{
			var mockQueueStorageUri = new Uri(QUEUE_STORAGE_URL);
			var storageCredentials = GetStorageCredentials();
			var mockQueueClient = new Mock<CloudQueueClient>(MockBehavior.Strict, mockQueueStorageUri, storageCredentials);
			mockQueueClient
				.Setup(c => c.GetQueueReference(mockQueue.Object.Name))
				.Returns(mockQueue.Object)
				.Verifiable();
			return mockQueueClient;
		}

		public static Mock<CloudQueue> GetMockQueue(string queueName)
		{
			var queueAddres = new Uri(QUEUE_STORAGE_URL + queueName);
			var mockQueue = new Mock<CloudQueue>(MockBehavior.Strict, queueAddres);
			mockQueue
				.Setup(c => c.CreateIfNotExistsAsync(It.IsAny<QueueRequestOptions>(), It.IsAny<OperationContext>(), It.IsAny<CancellationToken>()))
				.ReturnsAsync(false)
				.Verifiable();
			return mockQueue;
		}

		public static Mock<CloudStorageAccount> GetMockStorageAccount(Mock<CloudBlobClient> mockBlobClient, Mock<CloudQueueClient> mockQueueClient)
		{
			var storageCredentials = GetStorageCredentials();
			var storageAccount = new Mock<CloudStorageAccount>(MockBehavior.Strict, storageCredentials, true);

			if (mockBlobClient != null)
			{
				storageAccount
					.Setup(s => s.CreateCloudBlobClient())
					.Returns(mockBlobClient.Object)
					.Verifiable();
			}

			if (mockQueueClient != null)
			{
				storageAccount
					.Setup(s => s.CreateCloudQueueClient())
					.Returns(mockQueueClient.Object)
					.Verifiable();
			}

			return storageAccount;
		}

		public static StorageCredentials GetStorageCredentials()
		{
			var accountAccessKey = Convert.ToBase64String(Encoding.UTF8.GetBytes("this_is_a_bogus_account_access_key"));
			var storageCredentials = new StorageCredentials("account_name", accountAccessKey);
			return storageCredentials;
		}
	}
}
