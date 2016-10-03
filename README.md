# Picton

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](http://jericho.mit-license.org/)
[![Build status](https://ci.appveyor.com/api/projects/status/9guqjro396ytudv3?svg=true)](https://ci.appveyor.com/project/Jericho/picton)
[![Coverage Status](https://coveralls.io/repos/github/Jericho/Picton/badge.svg?branch=master)](https://coveralls.io/github/Jericho/Picton?branch=master)
[![CodeFactor](https://www.codefactor.io/repository/github/jericho/picton/badge)](https://www.codefactor.io/repository/github/jericho/picton)

## About

Picton is a library intendent to make it easier to work with Azure storage. 

The main features in this library are:

#### 1) Blob extension metods:
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
In release 7.0 of the Azure Storage library, Microsoft unsealed most classes and marked most methods as virtual which is quite significant because it allows mocking these classes when they are injected in one of your own classes. 
However, there remains a few sealed classes and a few non-virtal methods. One example where a non-virtual method prevents mocking is [discussed here](https://github.com/Azure/azure-storage-net/issues/318) and one example where a sealed class makes mocking quite difficult is [discussed here](https://github.com/Azure/azure-storage-net/issues/335).
I created abstractions for the classes in question in order to allow full mocking but I expect this problem to be resolved in a future release of the Azure Storage library which will make the the abstractions in the Picton library obsolete.
Specificaly, the library contains the following interfaces:

- IBlobClient
- IQueueClient
- IStorageAccount

An upcoming release will also include the following interfaces:

- IFileClient
- ITableClient

#### 3) Managers
The Blob and Queue managers are helpers that simplify common blob and queue related tasks. 
For example, the QueueManager automatically serializes and stores a message to a temporary location if the message exceeds the maximum size allowed in an Azure queue.
Another example: the Blob queue can automatically request a lock (AKA lease) before attempting to modify the content of a blob and it automatically releases the lock once the operation is completed.

#### 4) Misc
- AzureEmulatorManager alows starting the Azure Storage Emulator which you may need prior to executing integration testing


## Nuget

Picton is available as a Nuget package.

[![NuGet Version](http://img.shields.io/nuget/v/Picton.svg)](https://www.nuget.org/packages/Picton/)


## Release Notes

+ **0.1**
    - Initial release


## Installation

The easiest way to include Picton in your C# project is by grabing the nuget package:

```
PM> Install-Package Picton
```

Once you have the Picton library properly referenced in your project, add the following namespace(s):

```
using Picton;            // This is always required
using Picton.Interfaces; // This is only required if you want to use the abstractions
using Picton.Managers;   // This is only required if you want to use BlobManager or QueueManager
```

## Usage


#### 1) Blob extension metods:
Fist of all, some boilerplate code necessary for the code samples below:

```
var cancellationToken = CancellationToken.None;
var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
var blobClient = storageAccount.CreateCloudBlobClient();
var container = blobClient.GetContainerReference("mycontainer");
await container.CreateIfNotExistsAsync().ConfigureAwait(false);
var blob = container.GetBlockBlobReference("MyBlob.txt");
```

Here are a few examples how to use the extnsion methods:
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


#### 2) Abstractions
Let's assume you have the following class:
```
public class Foo
{
    private readonly CloudStorageAccount _cloudStorageAccount;
    public Foo(CloudStorageAccount cloudStorageAccount)
    {
        _cloudStorageAccount = cloudStorageAccount;
    }
}
```
This seems reasonable since the dependency is injected and presumably can be mocked for unit testing. Unfortunately, CloudStorageAccount is `sealed` and cannot be mocked.

Here's another example to illustrate the problem:
```
public class Foo
{
    private readonly CloudBlobClient _cloudBlobClient;
    public Foo(CloudBlobClient cloudBlobClient)
    {
        _cloudBlobClient = cloudBlobClient;
    }
    public void Bar()
    {
        var container = _cloudBlobClient.GetContainerReference("mycontainer");
        ... more code ...
    } 
}
```
In Azure Storage SDK v7.0, the CloudBlobClient class was unsealed which means that we can inject a mock blob client but the `GetContainerReference` method is not marked as `virtual` and therefore cannot be mocked.

Picton solves these problems by including interfaces and concrete implementations of the StorageAccount and BlobClient classes. This means that you can rewrite the two above examples like so:

`Example 1:`
```
public class Foo1
{
    private readonly IStorageAccount _storageAccount;
    public Foo(IStorageAccount storageAccount)
    {
        _storageAccount = storageAccount;
    }
}
```

`Example 2:`
```
public class Foo2
{
    private readonly IBlobClient _blobClient;
    public Foo(IBlobClient blobClient)
    {
        _blobClient = blobClient;
    }
    public void Bar()
    {
        var container = _blobClient.GetContainerReference("mycontainer");
        ... more code ...
    } 
}
```
You can use your favorite mocking tool to inject a mocked instance of IStorageAccount and IBlobClient in your unit tests and also you can pass a concrete instance like so:
```
var cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
var storageAccount = new StorageAccount(cloudStorageAccount);
var myFoo1 = new Foo1(storageAccount);

var blobClient = storageAccount.CreateCloudBlobClient();
var myFoo2 = new Foo2(blobClient);

```


#### 3) Managers

```
var cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
var storageAccount = new StorageAccount(cloudStorageAccount);
var blobManager = new BlobManager("mycontainer", storageAccount);
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

#### 4) Misc

```
class Program
{
    static void Main()
    {
        AzureEmulatorManager.EnsureStorageEmulatorIsStarted();

        var cloudStorageAccount = CloudStorageAccount.DevelopmentStorageAccount;
        var blobClient = cloudStorageAccount.CreateCloudBlobClient();
        var container = blobClient.GetContainerReference("mycontainer");
        await container.CreateIfNotExistsAsync().ConfigureAwait(false);
    }
}
```
