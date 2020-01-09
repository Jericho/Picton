# Picton

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](http://jericho.mit-license.org/)
[![Build status](https://ci.appveyor.com/api/projects/status/9guqjro396ytudv3?svg=true)](https://ci.appveyor.com/project/Jericho/picton)
[![Coverage Status](https://coveralls.io/repos/github/Jericho/Picton/badge.svg?branch=master)](https://coveralls.io/github/Jericho/Picton?branch=master)
[![CodeFactor](https://www.codefactor.io/repository/github/jericho/picton/badge)](https://www.codefactor.io/repository/github/jericho/picton)
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bhttps%3A%2F%2Fgithub.com%2FJericho%2FPicton.svg?type=shield)](https://app.fossa.io/projects/git%2Bhttps%3A%2F%2Fgithub.com%2FJericho%2FPicton?ref=badge_shield)

## About

Picton is a library intendent to make it easier to work with Azure storage. 

The main features in this library are:

#### 1) Extension metods:
The extension methods allow operations on blob while holding a lock (also known as a 'lease'). Specifically:

- Lock a blob for a given period of time with retries in case it is already locked by another process
- Extend an existing lease
- Release an existing lease
- Overwrite the content of a blob with a given string (there are also similar methods to upload a byte array or a stream)
- Append a given string to a blob (there are also similar methods to append a byte array or a stream)
- Update the metadata associated with a blob
- Download the content of a blob to a `string` (there is also a similar method to download the content to a `byte[]`)
- Make a copy of a blob
- Get a URI which can be used to gain access to a blob for a limid period of time


#### 2) Abstractions:
Early versions of the Picton library contained several interfaces to overcome the fact that most classed in the Azure Storage library where sealed and/or their methods where not marked as virtual and therefore not "mockable".
In [release 7.0 of the Azure Storage library](https://github.com/Azure/azure-storage-net/releases/tag/v7.0.0), Microsoft unsealed most classes and marked most methods as virtual which is quite significant because it allows mocking these classes when they are injected in one of your own classes. 
The Azure Storage library was further improved in [version 8.0](https://github.com/Azure/azure-storage-net/releases/tag/v8.0.0) to update the `Get*Reference` methods with the "virtual" qualifier.
The Azure Storage library was again improved in [version 9.0](https://github.com/Azure/azure-storage-net/releases/tag/v9.0.0) to unseal the `StorageAccount` class. This was the last hurdle that prevented "mocking" the Azure storage library in unit tests.
This means that all interfaces and wrapper classes in the Picton library have become obsolete and have ben removed in version 3.0.

#### 3) Managers
The Blob and Queue managers are helpers that simplify common blob and queue related tasks. 
For example, the QueueManager automatically serializes and stores a message to a temporary location if the message exceeds the maximum size allowed in an Azure queue.
Another example: the Blob queue can automatically request a lock (AKA lease) before attempting to modify the content of a blob and it automatically releases the lock once the operation is completed.

#### 4) Misc
- AzureEmulatorManager alows starting the Azure Storage Emulator which you may need prior to executing integration testing


## Nuget

Picton is available as a Nuget package.

[![NuGet Version](http://img.shields.io/nuget/v/Picton.svg)](https://www.nuget.org/packages/Picton/)


## Installation

The easiest way to include Picton in your C# project is by grabing the nuget package:

```
PM> Install-Package Picton
```

Once you have the Picton library properly referenced in your project, add the following namespace(s):

```
using Picton;            // This is always required
using Picton.Managers;   // This is only required if you want to use BlobManager or QueueManager
```

## Usage


#### 1) Blob extension metods:
Fist of all, some boilerplate code necessary for the code samples below:

```
var cancellationToken = CancellationToken.None;
var connectionString = "UseDevelopmentStorage=true";

var container = new BlobContainerClient(connectionString, "mycontainer");
await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken).ConfigureAwait(false);
var blob = container.GetBlockBlobClient("MyBlob.txt");
```

Here are a few examples how to use the extension methods:
```
var leaseId = await blob.TryAcquireLeaseAsync(TimeSpan.FromSeconds(15), 5, cancellationToken).ConfigureAwait(false);
await blob.UploadTextAsync("Hello World", leaseId, cancellationToken).ConfigureAwait(false);
await blob.AppendTextAsync("More content", leaseId, cancellationToken).ConfigureAwait(false);
await blob.AppendTextAsync("Even more content", leaseId, cancellationToken).ConfigureAwait(false);
await blob.TryRenewLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);
await blob.AppendTextAsync("More more more", leaseId, cancellationToken).ConfigureAwait(false);
await blob.ReleaseLeaseAsync(leaseId, cancellationToken).ConfigureAwait(false);

var content = await blob.DownloadTextAsync(cancellationToken).ConfigureAwait(false);
await blob.CopyAsync("MyCopy.txt", cancellationToken).ConfigureAwait(false);

var permission = SharedAccessBlobPermissions.Read;
var duration = TimeSpan.FromMinutes(30);
var accessUri = await blob.GetSharedAccessSignatureUri(permission, duration).ConfigureAwait(false);
```


#### 2) Managers

```
var connectionString = "UseDevelopmentStorage=true";
var blobManager = new BlobManager(connectionString, "mycontainer");

await blobManager.CopyBlobAsync("test.txt", "test - Copy of.txt", cancellationToken).ConfigureAwait(false);

await blobManager.UploadTextAsync("test2.txt", "Hello World", cancellationToken: cancellationToken).ConfigureAwait(false);
await blobManager.AppendTextAsync("test2.txt", "qwerty", cancellationToken: cancellationToken).ConfigureAwait(false);

foreach (var blob in blobManager.ListBlobs("test", false, false))
{
    Console.WriteLine(blob.Uri.AbsoluteUri);
}

await blobManager.DeleteBlobAsync("test - Copy of.txt", cancellationToken).ConfigureAwait(false);
await blobManager.DeleteBlobsWithPrefixAsync("test", cancellationToken).ConfigureAwait(false);
```

#### 3) Misc

```
class Program
{
    static void Main()
    {
        AzureEmulatorManager.EnsureStorageEmulatorIsStarted();

        var cancellationToken = CancellationToken.None;
        var connectionString = "UseDevelopmentStorage=true";
        var container = new BlobContainerClient(connectionString, "mycontainer");
		await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken).ConfigureAwait(false);
    }
}
```


## License
[![FOSSA Status](https://app.fossa.io/api/projects/git%2Bhttps%3A%2F%2Fgithub.com%2FJericho%2FPicton.svg?type=large)](https://app.fossa.io/projects/git%2Bhttps%3A%2F%2Fgithub.com%2FJericho%2FPicton?ref=badge_large)
