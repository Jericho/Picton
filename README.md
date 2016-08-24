# Picton

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](http://jericho.mit-license.org/)
[![Build status](https://ci.appveyor.com/api/projects/status/9guqjro396ytudv3?svg=true)](https://ci.appveyor.com/project/Jericho/picton)
[![Coverage Status](https://coveralls.io/repos/github/Jericho/Picton/badge.svg?branch=master)](https://coveralls.io/github/Jericho/Picton?branch=master)

## About

Picton is a library intendent to make it easier to work with Azure storage. 

The main features in this library are:

#### Blob extension metods:
For instance, it contains extension methods to append the content of a string/byte array/stream to an existing blob, to get a lock (AKA lease) on an existing blob, to perform operations on a blob only if a lease can be obtained, etc.

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

Once you have the Picton library properly referenced in your project, add the following namespace:

```
using Picton;
```

## Usage

