using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;

namespace Picton.Interfaces
{
	public interface IStorageAccount
	{
		CloudBlobClient CreateCloudBlobClient();
		CloudFileClient CreateCloudFileClient();
		CloudQueueClient CreateCloudQueueClient();
		CloudTableClient CreateCloudTableClient();
		string GetSharedAccessSignature(SharedAccessAccountPolicy policy);
		string ToString(bool exportSecrets);
	}
}
