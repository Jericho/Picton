using Azure;
using Azure.Storage.Queues.Models;
using NSubstitute;
using Picton.Managers;
using Shouldly;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Picton.UnitTests.Managers
{
	public class MultiTenantQueueMangerTests
	{
		[Fact]
		public void Throws_when_factory_is_null()
		{
			// Arrange
			Func<string, QueueManager> queueManagerFactory = null;

			// Act
			Should.Throw<Exception>(() => new MultiTenantQueueManager(queueManagerFactory));
		}

		[Fact]
		public async Task Small_message()
		{
			// Arrange
			var mockBlobContainer = MockUtils.GetMockBlobContainerClient("mycontainer-1", null);
			var mockQueueClient = MockUtils.GetMockQueueClient("myqueue-1");

			mockQueueClient
				.SendMessageAsync(Arg.Any<string>(), Arg.Any<TimeSpan?>(), Arg.Any<TimeSpan?>(), Arg.Any<CancellationToken>())
				.Returns(callInfo =>
				{
					var receipt = QueuesModelFactory.SendReceipt("myMessageId", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(1), "myPopReceipt", DateTimeOffset.UtcNow.AddMinutes(5));
					return Response.FromValue(receipt, new MockAzureResponse(200, "ok"));
				});

			QueueManager queueManagerFactory(string tenantId)
			{
				if (tenantId != "1") throw new Exception("This unit test is designed to only test tenant Id '1'");
				return new QueueManager(mockBlobContainer, mockQueueClient, true);
			}

			// Act
			var queueManager = new MultiTenantQueueManager(queueManagerFactory);
			await queueManager.AddMessageAsync("1", "Hello world!");

			// Assert
		}
	}
}
