# Picton

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](http://jericho.mit-license.org/)
[![Build status](https://ci.appveyor.com/api/projects/status/9guqjro396ytudv3?svg=true)](https://ci.appveyor.com/project/Jericho/picton)
[![Coverage Status](https://coveralls.io/repos/github/Jericho/Picton/badge.svg?branch=master)](https://coveralls.io/github/Jericho/Picton?branch=master)

## About

Picton is a library intendent to make it easier to work with Azure storage. 

The main features in this library are:

#### Blob extension metods:
The extension methods allow operations on blob while holding a lock (also known as a 'lease'). Specifically:

- Lock a blob for a givenperiod of time with retries in case it is already locked by another proces
- Eextend an existing lease
- Release an existing lease
- Overwrite the content of a blob with a given string (there are also similar methods to upload a byte array and a stream)
- Append a given string to a blob (there are also similar methods to append a byte array and a stream)
- Update the metadata associated with a blob
- Download the content of a blob to a `string` (there is also a similar method to download the content to a `byte[]`)
- Make a copy of a blob
- Get a URI which can be used to gain access to a blob for a limid period of time


#### Abstractions:
In release 7.0 of the Azure Storage library, Microsoft unsealed most classes and marked most methods as virtual which is quite significant because it allows mocking these classes when they are injected in one of your own classes. 
However, there remains a few sealed classes and a few non-virtal methods. One example where a non-virtual method prevents mocking is [discussed here](https://github.com/Azure/azure-storage-net/issues/318) and one example where a sealed class makes mocking quite difficult is [discussed here](https://github.com/Azure/azure-storage-net/issues/335).
I created abstractions for the classes in question in order to allow full mocking but I expect this problem to be resolved in a future release of the Azure Storage library which will make the the abstractions in the Picton library obsolete.

#### Blob and Queue managers
The Blob and Queue managers ar helpers that simplify common blob and queue related tasks. 
For example, the QueueManager automatically serializes and stores a message to a temporary location if the message exceeds the maximum size allowed in an Azure queue.
Another example: the Blob queue can automatically request a lock (AKA lease) before attempting to modify the content of a blob and it automatically releases the lock once the operation is completed.

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


#### Blob extension metods:
Fist of all, some boilerplate code necessary for the code samples below:

```
var storageAccount = CloudStorageAccount.DevelopmentStorageAccount;
var blobClient = storageAccount.CreateCloudBlobClient();
var container = blobClient.GetContainerReference("mycontainer");
await container.CreateIfNotExistsAsync().ConfigureAwait(false);
var blob = container.GetBlobkBlobRference("MyBlob.txt");
```

Here are a few examples how to use the extnsion methods:
```
var maxRetries = 5;
var leaseTimeout = TimeSpan.FromSeconds(15);
var leaseId = await blob.TryAcquireLeaseAsync(leaseTimeout, maxRetries, CancellationToken.None).ConfigureAwait(false);
await blob.UploadTextAsync("Hello World", leaseId, CancellationToken.None).ConfigureAwait(false);
await blob.AppendTextAsync("More content", leaseId, CancellationToken.None).ConfigureAwait(false);
await blob.AppendTextAsync("Even more content", leaseId, CancellationToken.None).ConfigureAwait(false);
await blob.TryRenewLeaseAsync(leaseId, CancellationToken.None).ConfigureAwait(false);
await blob.AppendTextAsync("Mo more more", leaseId, CancellationToken.None).ConfigureAwait(false);
await blob.ReleaseLeaseAsync(leaseId, CancellationToken.None).ConfigureAwait(false);

var content = await blob.DownloadTextAsync(CancellationToken.None).ConfigureAwait(false);
await blob.CopyAsync("MyCopy.txt", CancellationToken.None).ConfigureAwait(false);

var permission = SharedAccessBlobPermissions.Read;
var duration = TimeSpan.FromMinutes(30);
var accessUri = await blob.GetSharedAccessSignatureUri(permission, duration).ConfigureAwait(false);
```
