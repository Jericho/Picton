using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Queue;

namespace Picton.Interfaces
{
	public interface IStorageAccount
	{
		CloudBlobClient CreateCloudBlobClient();
		//IFileClient CreateCloudFileClient();
		CloudQueueClient CreateCloudQueueClient();
		//ITableClient CreateCloudTableClient();
		string GetSharedAccessSignature(SharedAccessAccountPolicy policy);
		string ToString(bool exportSecrets);
	}
}
