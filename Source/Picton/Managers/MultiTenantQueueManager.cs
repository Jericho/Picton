using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Picton.Interfaces;
using Picton.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Picton.Managers
{
	public class MultiTenantQueueManager : IMultiTenantQueueManager
	{
		private readonly ISystemClock _systemClock;
		private readonly IRandomGenerator _randomGenerator;
		private readonly Func<string, QueueManager> _queueManagerFactory;
		private readonly ConcurrentDictionary<string, Lazy<QueueManager>> _tenantQueueManagers = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="MultiTenantQueueManager"/> class.
		/// </summary>
		/// <param name="connectionString">
		/// A connection string includes the authentication information
		/// required for your application to access data in an Azure Storage
		/// account at runtime.
		///
		/// For more information, see
		/// <see href="https://docs.microsoft.com/azure/storage/common/storage-configure-connection-string">
		/// Configure Azure Storage connection strings</see>.
		/// </param>
		/// <param name="queuePrefix">The part of the queueN name that preceeds the tenantId in the storage account.</param>
		/// <param name="queueClientOptions">
		/// Optional client options that define the transport pipeline
		/// policies for authentication, retries, etc., that are applied to
		/// every request to the queue.
		/// </param>
		/// <param name="blobClientOptions">
		/// Optional client options that define the transport pipeline
		/// policies for authentication, retries, etc., that are applied to
		/// every request to the blob storage.
		/// </param>
		/// <param name="systemClock">Allows dependency injection of a clock for unit tesing purposes. Feel free to ignore this parameter.</param>
		/// <param name="randomGenerator">Allows dependency injection of a random number generator for unit tesing purposes. Feel free to ignore this parameter.</param>
		[ExcludeFromCodeCoverage]
		public MultiTenantQueueManager(string connectionString, string queuePrefix, QueueClientOptions queueClientOptions = null, BlobClientOptions blobClientOptions = null, ISystemClock systemClock = null, IRandomGenerator randomGenerator = null)
		{
			if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
			if (string.IsNullOrEmpty(queuePrefix)) throw new ArgumentNullException(nameof(queuePrefix));

			_queueManagerFactory = (tenantId) =>
			{
				var blobContainerClient = new BlobContainerClient(connectionString, $"{queuePrefix}{tenantId}-oversized-messages", blobClientOptions);
				var queueClient = new QueueClient(connectionString, $"{queuePrefix}{tenantId}", queueClientOptions);
				return new QueueManager(blobContainerClient, queueClient, false, _systemClock, _randomGenerator);
			};

			_systemClock = systemClock ?? SystemClock.Instance;
			_randomGenerator = randomGenerator ?? RandomGenerator.Instance;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="MultiTenantQueueManager"/> class.
		/// </summary>
		/// <param name="queueManagerFactory">Factory method to instantiate a QueueManager for a given tenant.</param>
		internal MultiTenantQueueManager(Func<string, QueueManager> queueManagerFactory)
		{
			_queueManagerFactory = queueManagerFactory ?? throw new ArgumentNullException(nameof(queueManagerFactory));

			_systemClock = SystemClock.Instance;
			_randomGenerator = RandomGenerator.Instance;
		}

		/// <inheritdoc/>
		public Task AddMessageAsync<T>(string tenantId, T message, IDictionary<string, string> metadata = null, TimeSpan? timeToLive = null, TimeSpan? initialVisibilityDelay = null, CancellationToken cancellationToken = default)
		{
			var tenantQueueManager = GetTenantQueueManager(tenantId);
			return tenantQueueManager.AddMessageAsync(message, metadata, timeToLive, initialVisibilityDelay, cancellationToken);
		}

		/// <inheritdoc/>
		public Task ClearTenantAsync(string tenantId, CancellationToken cancellationToken = default)
		{
			var tenantQueueManager = GetTenantQueueManager(tenantId);
			return tenantQueueManager.ClearAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public Task DeleteTenantAsync(string tenantId, CancellationToken cancellationToken = default)
		{
			var tenantQueueManager = GetTenantQueueManager(tenantId);
			return tenantQueueManager.DeleteResourcesAsync(cancellationToken);
		}

		/// <inheritdoc/>
		public Task DeleteMessageAsync(string tenantId, CloudMessage message, CancellationToken cancellationToken = default)
		{
			var tenantQueueManager = GetTenantQueueManager(tenantId);
			return tenantQueueManager.DeleteMessageAsync(message, cancellationToken);
		}

		/// <inheritdoc/>
		public Task<CloudMessage[]> GetMessagesAsync(string tenantId, int messageCount, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default)
		{
			var tenantQueueManager = GetTenantQueueManager(tenantId);
			return tenantQueueManager.GetMessagesAsync(messageCount, visibilityTimeout, cancellationToken);
		}

		private QueueManager GetTenantQueueManager(string tenantId)
		{
			var lazyQueueManager = _tenantQueueManagers.GetOrAdd(tenantId, tenantId =>
			{
				return new Lazy<QueueManager>(() =>
				{
					return _queueManagerFactory.Invoke(tenantId);
				});
			});

			return lazyQueueManager.Value;
		}
	}
}
