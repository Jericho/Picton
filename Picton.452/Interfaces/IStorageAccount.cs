using Microsoft.WindowsAzure.Storage;

namespace Picton.Interfaces
{
	public interface IStorageAccount
	{
		IBlobClient CreateCloudBlobClient();
		//IFileClient CreateCloudFileClient();
		IQueueClient CreateCloudQueueClient();
		//ITableClient CreateCloudTableClient();
		string GetSharedAccessSignature(SharedAccessAccountPolicy policy);
		string ToString(bool exportSecrets);
	}
}
