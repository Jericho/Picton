using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Queues;
using Picton.Managers;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.IntegrationTests
{
	class Program
	{
		static async Task Main()
		{
			// Make sure the emulators are started
			//Console.WriteLine("Please wait: making sure the Storage emulator is started. This is typically very quick.");
			//AzureEmulatorManager.EnsureStorageEmulatorIsStarted();

			//Console.WriteLine("Please wait: making sure the DocumentDB emulator is started. This can take several seconds.");
			//AzureEmulatorManager.EnsureDocumentDbEmulatorIsStarted();

			using (var emulator = new AzuriteManager())
			{
				var cancellationToken = CancellationToken.None;
				var connectionString = "UseDevelopmentStorage=true";
				var containerName = "mycontainer";
				var queueName = "myqueue";

				// Make sure the container is created
				var container = new BlobContainerClient(connectionString, containerName);
				await container.CreateIfNotExistsAsync().ConfigureAwait(false);

				// Run the integration tests (they are dependant on the Azure Storage emulator)
				Console.WriteLine("Running blob extension methods tests...");
				await RunCloudBlobExtensionsTests(connectionString, containerName, cancellationToken).ConfigureAwait(false);

				Console.WriteLine("Running blob manager tests...");
				await RunBlobManagerTests(connectionString, containerName, cancellationToken).ConfigureAwait(false);

				Console.WriteLine("Running queue manager tests...");
				await RunQueueManagerTests(connectionString, queueName, cancellationToken).ConfigureAwait(false);
			}

			// Flush the console key buffer
			while (Console.KeyAvailable) Console.ReadKey(true);

			// Wait for user to press a key
			Console.WriteLine("\r\nPress any key to exit...");
			Console.ReadKey();
		}

		private static async Task RunCloudBlobExtensionsTests(string connectionString, string containerName, CancellationToken cancellationToken)
		{
			// BlobClient
			var blob1 = new BlobClient(connectionString, containerName, "test1.txt");
			var exists1 = await blob1.ExistsAsync(cancellationToken).ConfigureAwait(false);
			if (!exists1) await blob1.CreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			var leaseId1 = await blob1.AcquireLeaseAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
			await blob1.UploadTextAsync("Hello World", leaseId: leaseId1, cancellationToken: cancellationToken).ConfigureAwait(false);
			await blob1.UploadTextAsync("qwerty", leaseId: leaseId1, cancellationToken: cancellationToken).ConfigureAwait(false);
			await blob1.AppendTextAsync("azerty", leaseId: leaseId1, cancellationToken: cancellationToken).ConfigureAwait(false);
			var secondLeaseId1 = await blob1.TryAcquireLeaseAsync(TimeSpan.FromSeconds(30), 1, cancellationToken).ConfigureAwait(false);
			if (secondLeaseId1 != null) throw new Exception("Getting a second lease on a blob should not work.");
			if (!string.IsNullOrEmpty(leaseId1)) await blob1.ReleaseLeaseAsync(leaseId1, cancellationToken).ConfigureAwait(false);
			var content1a = await blob1.DownloadTextAsync(cancellationToken).ConfigureAwait(false);
			var content1b = await blob1.DownloadByteArrayAsync(cancellationToken).ConfigureAwait(false);
			Console.WriteLine($"Content of BlobClient: {content1a}");


			// BlockBlobClient
			var blob2 = new BlockBlobClient(connectionString, containerName, "test2.txt");
			var exists2 = await blob2.ExistsAsync(cancellationToken).ConfigureAwait(false);
			if (!exists2) await blob2.CreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			var leaseId2 = await blob2.AcquireLeaseAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
			await blob2.UploadTextAsync("Hello World", leaseId: leaseId2, cancellationToken: cancellationToken).ConfigureAwait(false);
			await blob2.UploadTextAsync("qwerty", leaseId: leaseId2, cancellationToken: cancellationToken).ConfigureAwait(false);
			await blob2.AppendTextAsync("azerty", leaseId: leaseId2, cancellationToken: cancellationToken).ConfigureAwait(false);
			var secondLeaseId2 = await blob2.TryAcquireLeaseAsync(TimeSpan.FromSeconds(30), 3, cancellationToken).ConfigureAwait(false);
			if (secondLeaseId2 != null) throw new Exception("Getting a second lease on a blob should not work.");
			if (!string.IsNullOrEmpty(leaseId2)) await blob2.ReleaseLeaseAsync(leaseId2, cancellationToken).ConfigureAwait(false);
			var content2a = await blob2.DownloadTextAsync(cancellationToken).ConfigureAwait(false);
			var content2b = await blob2.DownloadByteArrayAsync(cancellationToken).ConfigureAwait(false);
			Console.WriteLine($"Content of BlockBlob: {content2a}");


			// PageBlobClient
			var blob3 = new PageBlobClient(connectionString, containerName, "test3.txt");
			var exists3 = await blob3.ExistsAsync(cancellationToken).ConfigureAwait(false);
			if (!exists3) await blob3.CreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			var leaseId3 = await blob3.AcquireLeaseAsync(TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
			await blob3.UploadTextAsync(new string('A', 535), leaseId: leaseId3, cancellationToken: cancellationToken).ConfigureAwait(false);
			await blob3.UploadTextAsync(new string('B', blob3.PageBlobPageBytes), leaseId: leaseId3, cancellationToken: cancellationToken).ConfigureAwait(false);
			await blob3.AppendTextAsync(new string('C', blob3.PageBlobPageBytes), leaseId: leaseId3, cancellationToken: cancellationToken).ConfigureAwait(false);
			var secondLeaseId3 = await blob3.TryAcquireLeaseAsync(TimeSpan.FromSeconds(30), 3, cancellationToken).ConfigureAwait(false);
			if (secondLeaseId3 != null) throw new Exception("Getting a second lease on a blob should not work.");
			if (!string.IsNullOrEmpty(leaseId3)) await blob3.ReleaseLeaseAsync(leaseId3, cancellationToken).ConfigureAwait(false);
			var content3a = await blob3.DownloadTextAsync(cancellationToken).ConfigureAwait(false);
			var content3b = await blob3.DownloadByteArrayAsync(cancellationToken).ConfigureAwait(false);
			Console.WriteLine($"Content of PageBlob: {content3a.Trim('\0')}"); // Trimming the content before writing to console is important because page blobs are padded with null characters.

			//===========================================================================================
			// Unfortunately, the emulator does not support AppendBlob
			/*
			var blob4 = new AppendBlobClient(connectionString, containerName, "test4.txt");
			var exists1 = await blob4.ExistsAsync(cancellationToken).ConfigureAwait(false);
			if (!exists4) await blob4.CreateAsync(cancellationToken: cancellationToken).ConfigureAwait(false);

			var leaseId4 = await blob4.AcquireLeaseAsync(TimeSpan.FromSeconds(40), cancellationToken).ConfigureAwait(false);
			await blob4.UploadTextAsync("Hello World", leaseId: leaseId4, cancellationToken: cancellationToken).ConfigureAwait(false);
			await blob4.UploadTextAsync("qwerty", leaseId: leaseId4, cancellationToken: cancellationToken).ConfigureAwait(false);
			await blob4.AppendTextAsync("azerty", leaseId: leaseId4, cancellationToken: cancellationToken).ConfigureAwait(false);
			var secondLeaseId4 = await blob4.TryAcquireLeaseAsync(TimeSpan.FromSeconds(40), 4, cancellationToken).ConfigureAwait(false);
			if (secondLeaseId4 != null) throw new Exception("Getting a second lease on a blob should not work.");
			if (!string.IsNullOrEmpty(leaseId4)) await blob4.ReleaseLeaseAsync(leaseId4, cancellationToken).ConfigureAwait(false);
			var content4a = await blob4.DownloadTextAsync(cancellationToken).ConfigureAwait(false);
			var content4b = await blob4.DownloadByteArrayAsync(cancellationToken).ConfigureAwait(false);
			Console.WriteLine($"Content of AppendBlob: {content4a}");
			*/
			//===========================================================================================
		}

		private static async Task RunBlobManagerTests(string connectionString, string containerName, CancellationToken cancellationToken)
		{
			var blobManager = new BlobManager(connectionString, containerName);

			await blobManager.CopyBlobAsync("test1.txt", "test1 - Copy of.txt", cancellationToken: cancellationToken).ConfigureAwait(false);
			await blobManager.CopyBlobAsync("test2.txt", "test2 - Copy of.txt", cancellationToken: cancellationToken).ConfigureAwait(false);

			await blobManager.UploadTextAsync("test4.txt", "Hello World", cancellationToken: cancellationToken).ConfigureAwait(false);
			await blobManager.AppendTextAsync("test4.txt", "\r\nqwerty", cancellationToken: cancellationToken).ConfigureAwait(false);
			await blobManager.AppendTextAsync("test4.txt", "\r\nazerty", cancellationToken: cancellationToken).ConfigureAwait(false);

			var blobs = blobManager.ListBlobs("test", false, cancellationToken);
			foreach (var blob in blobs)
			{
				Console.WriteLine(blob.Name);
			}

			await blobManager.DeleteBlobAsync("test1 - Copy of.txt", cancellationToken).ConfigureAwait(false);
			await blobManager.DeleteBlobAsync("test2 - Copy of.txt", cancellationToken).ConfigureAwait(false);

			await blobManager.DeleteBlobsWithPrefixAsync("test", cancellationToken).ConfigureAwait(false);
		}

		private static async Task RunQueueManagerTests(string connectionString, string queueName, CancellationToken cancellationToken)
		{
			var queueManager = new QueueManager(connectionString, queueName);

			// Empty the queue
			await queueManager.ClearAsync(cancellationToken).ConfigureAwait(false);

			// Check that the queue is empty
			var queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 0) throw new Exception($"We expected the queue to be empty but we found {queuedMessagesCount} messages.");


			//-----------------------------------------------------------------
			// Send a simple message
			var sample = new SampleMessageType
			{
				StringProp = "abc123",
				IntProp = 123,
				GuidProp = Guid.NewGuid(),
				DateProp = new DateTime(2016, 10, 6, 1, 2, 3, DateTimeKind.Utc)
			};
			await queueManager.AddMessageAsync(sample).ConfigureAwait(false);

			// Check that there is one message in the queue
			queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 1) throw new Exception($"We expected only one message in the queue but we found {queuedMessagesCount} messages.");

			// Get the message
			var message1 = await queueManager.GetMessageAsync(TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
			if (message1.Content.GetType() != typeof(SampleMessageType)) throw new Exception("The type of the received message does not match the expected type");
			var receivedMessage = (SampleMessageType)message1.Content;
			if (receivedMessage.StringProp != sample.StringProp) throw new Exception("Did not receive the expected message");
			if (receivedMessage.IntProp != sample.IntProp) throw new Exception("Did not receive the expected message");
			if (receivedMessage.GuidProp != sample.GuidProp) throw new Exception("Did not receive the expected message");
			if (receivedMessage.DateProp != sample.DateProp) throw new Exception("Did not receive the expected message");

			// Delete the message from the queue
			await queueManager.DeleteMessageAsync(message1).ConfigureAwait(false);

			// Check that the queue is empty
			queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 0) throw new Exception($"We expected the queue to be empty but we found {queuedMessagesCount} messages.");


			//-----------------------------------------------------------------
			// Send a message that exceeds the max size allowed in Azure queues
			int characterCount = 100000;
			var largeSample = new SampleMessageType
			{
				StringProp = RandomGenerator.GenerateString(characterCount)
			};
			await queueManager.AddMessageAsync(largeSample).ConfigureAwait(false);

			// Check that there is one message in the queue
			queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 1) throw new Exception($"We expected only one message in the queue but we found {queuedMessagesCount} messages.");

			// Get the message
			var message2 = await queueManager.GetMessageAsync(TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
			if (message2.Content.GetType() != typeof(SampleMessageType)) throw new Exception("The type of the received message does not match the expected type");
			var largeMessage = (SampleMessageType)message2.Content;
			if (largeMessage.StringProp.Length != characterCount) throw new Exception("Did not receive the expected message");

			// Delete the message from the queue
			await queueManager.DeleteMessageAsync(message2).ConfigureAwait(false);

			// Check that the queue is empty
			queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 0) throw new Exception($"We expected the queue to be empty but we found {queuedMessagesCount} messages.");


			//-----------------------------------------------------------------
			// Send a simple string
			await queueManager.AddMessageAsync("Hello World").ConfigureAwait(false);

			// Check that there is one message in the queue
			queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 1) throw new Exception($"We expected only one message in the queue but we found {queuedMessagesCount} messages.");

			// Get the message
			var message3 = await queueManager.GetMessageAsync(TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false);
			if (message3.Content.GetType() != typeof(string)) throw new Exception("The type of the received message does not match the expected type");
			if ((string)message3.Content != "Hello World") throw new Exception("Did not receive the expected message");

			// Delete the message from the queue
			await queueManager.DeleteMessageAsync(message3).ConfigureAwait(false);

			// Check that the queue is empty
			queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 0) throw new Exception($"We expected the queue to be empty but we found {queuedMessagesCount} messages.");


			//-----------------------------------------------------------------
			// Send messages using the Azure CloudQueue class
			// thereby bypassing Picton's queue manager serialization
			var queue = new QueueClient(connectionString, queueName);
			await queue.SendMessageAsync("Hello World STRING 1", cancellationToken).ConfigureAwait(false);
			await queue.SendMessageAsync("Hello World STRING 2", cancellationToken).ConfigureAwait(false);
			await queue.SendMessageAsync("Hello World STRING 3", cancellationToken).ConfigureAwait(false);

			// Check that there are three messages in the queue
			queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 3) throw new Exception($"We expected three messages in the queue but we found {queuedMessagesCount} messages.");

			// Get the messages
			var messages = (await queueManager.GetMessagesAsync(10, TimeSpan.FromMinutes(5), cancellationToken).ConfigureAwait(false)).ToArray();
			if (messages[0].Content.GetType() != typeof(string)) throw new Exception("The type of the received message does not match the expected type");
			if ((string)messages[0].Content != "Hello World STRING 1") throw new Exception("Did not receive the expected message");
			if (messages[1].Content.GetType() != typeof(string)) throw new Exception("The type of the received message does not match the expected type");
			if ((string)messages[1].Content != "Hello World STRING 2") throw new Exception("Did not receive the expected message");
			if (messages[2].Content.GetType() != typeof(string)) throw new Exception("The type of the received message does not match the expected type");
			if ((string)messages[2].Content != "Hello World STRING 3") throw new Exception("Did not receive the expected message");

			// Clear the queue
			await queueManager.ClearAsync().ConfigureAwait(false);

			// Check that the queue is empty
			queuedMessagesCount = await queueManager.GetApproximateMessageCountAsync().ConfigureAwait(false);
			if (queuedMessagesCount != 0) throw new Exception($"We expected the queue to be empty but we found {queuedMessagesCount} messages.");
		}
	}
}
