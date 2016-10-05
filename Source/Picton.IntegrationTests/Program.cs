using Microsoft.WindowsAzure.Storage;
using Picton.Interfaces;
using Picton.Managers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.IntegrationTests
{
	class Program
	{
		static void Main()
		{
			// Make sure the Azure Storage emulator is started
			AzureEmulatorManager.EnsureStorageEmulatorIsStarted();

			var cancellationToken = CancellationToken.None;
			var storageAccount = new StorageAccount(CloudStorageAccount.DevelopmentStorageAccount);
			var containerName = "mycontainer";

			// Run the integration tests (they are dependant on the Azure Storage emulator)
			CloudBlobExtensions(storageAccount, containerName, cancellationToken).Wait();
			BlobManager(storageAccount, containerName, cancellationToken).Wait();

			// Flush the console key buffer
			while (Console.KeyAvailable) Console.ReadKey(true);

			// Wait for user to press a key
			Console.WriteLine("\r\nPress any key to exit...");
			Console.ReadKey();
		}

		private static async Task CloudBlobExtensions(IStorageAccount storageAccount, string containerName, CancellationToken cancellationToken)
		{
			var blobClient = storageAccount.CreateCloudBlobClient();
			var container = blobClient.GetContainerReference(containerName);
			await container.CreateIfNotExistsAsync().ConfigureAwait(false);

			var blob1 = container.GetBlockBlobReference("test1.txt");
			var leaseId1 = (await blob1.ExistsAsync(cancellationToken).ConfigureAwait(false) ? await blob1.TryAcquireLeaseAsync(null, 3, cancellationToken).ConfigureAwait(false) : null);
			await blob1.UploadTextAsync("Hello World", leaseId1, cancellationToken);
			await blob1.UploadTextAsync("qwerty", leaseId1, cancellationToken);
			await blob1.AppendTextAsync("azerty", leaseId1, cancellationToken);
			if (!string.IsNullOrEmpty(leaseId1)) await blob1.ReleaseLeaseAsync(leaseId1, cancellationToken);
			var content1 = await blob1.DownloadTextAsync(cancellationToken).ConfigureAwait(false);
			Console.WriteLine(content1);

			var blob2 = container.GetPageBlobReference("test2.txt");
			var leaseId2 = (await blob2.ExistsAsync(cancellationToken).ConfigureAwait(false) ? await blob2.TryAcquireLeaseAsync(null, 3, cancellationToken).ConfigureAwait(false) : null);
			await blob2.UploadTextAsync(new string('A', 512), leaseId2, cancellationToken).ConfigureAwait(false);
			await blob2.UploadTextAsync(new string('B', 512), leaseId2, cancellationToken).ConfigureAwait(false);
			await blob2.AppendTextAsync(new string('C', 512), leaseId2, cancellationToken).ConfigureAwait(false);
			if (!string.IsNullOrEmpty(leaseId2)) await blob2.ReleaseLeaseAsync(leaseId2, cancellationToken).ConfigureAwait(false);
			var content2 = await blob2.DownloadTextAsync(cancellationToken).ConfigureAwait(false);
			Console.WriteLine(content2);

			// Unfortunately, the emulator does not support CloudAppendBlob
			//var blob3 = container.GetAppendBlobReference("test3.txt");
			//blob3.UploadTextAsync("Hello World", null, cancellationToken).Wait();
			//blob3.UploadTextAsync("\r\nqwerty", null, cancellationToken).Wait();
			//blob3.AppendTextAsync("azerty", null, cancellationToken).Wait();
			//var task3 = blob3.DownloadTextAsync(cancellationToken);
			//task3.Wait();
			//var content3 = task3.Result;
			//Console.WriteLine(content3);
		}

		private static async Task BlobManager(IStorageAccount storageAccount, string containerName, CancellationToken cancellationToken)
		{
			var blobManager = new BlobManager(containerName, storageAccount);

			await blobManager.CopyBlobAsync("test1.txt", "test1 - Copy of.txt", cancellationToken).ConfigureAwait(false);
			await blobManager.CopyBlobAsync("test2.txt", "test2 - Copy of.txt", cancellationToken).ConfigureAwait(false);

			await blobManager.UploadTextAsync("test4.txt", "Hello World", cancellationToken: cancellationToken).ConfigureAwait(false);
			await blobManager.AppendTextAsync("test4.txt", "\r\nqwerty", cancellationToken: cancellationToken).ConfigureAwait(false);
			await blobManager.AppendTextAsync("test4.txt", "\r\nazerty", cancellationToken: cancellationToken).ConfigureAwait(false);

			foreach (var blob in blobManager.ListBlobs("test1", false, false))
			{
				Console.WriteLine(blob.Uri.AbsoluteUri);
			}

			await blobManager.DeleteBlobAsync("test1 - Copy of.txt", cancellationToken).ConfigureAwait(false);
			await blobManager.DeleteBlobAsync("test2 - Copy of.txt", cancellationToken).ConfigureAwait(false);

			await blobManager.DeleteBlobsWithPrefixAsync("test", cancellationToken).ConfigureAwait(false);
		}
	}
}
