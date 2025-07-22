# LocalPrefs
[![dotnet-test](https://github.com/AndanteTribe/LocalPrefs/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/LocalPrefs/actions/workflows/dotnet-test.yml)
[![unity-test](https://github.com/AndanteTribe/LocalPrefs/actions/workflows/unity-test.yml/badge.svg)](https://github.com/AndanteTribe/LocalPrefs/actions/workflows/unity-test.yml)
[![Releases](https://img.shields.io/github/release/AndanteTribe/LocalPrefs.svg)](https://github.com/AndanteTribe/LocalPrefs/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/LocalPrefs.svg)](./LICENSE)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/AndanteTribe/LocalPrefs)

English | [日本語](README_JA.md)

## Overview
**LocalPrefs** is a library that provides local save/load functionality for .NET and Unity.

> [!CAUTION]
> This library is currently provided as a preview version.
> The installation procedure is not well developed.

Unity’s built-in `UnityEngine.PlayerPrefs` API is known for several critical issues, including:

1. On Windows, data is saved to the system registry.
2. On Web platforms, data is stored in IndexedDB, but the key includes an inexplicable hash that may change with each build under certain conditions. This behavior can interfere with game updates or pollute IndexedDB with entries using different hashed keys.
3. On Web platforms, the storage limit is capped at 1MB.
4. On Web platforms, saving is not immediate and is instead asynchronous, making it difficult to determine when data is actually persisted.
5. The supported data types are limited (only `int`, `float`, and `string`).

**LocalPrefs** addresses these problems and offers a high-performance implementation.

1. When using `LocalPrefs.Shared`, the default save path is `Application.persistentDataPath` in Unity. In .NET environments, an equivalent persistent path is used.
2. It provides APIs that allow customization, such as defining save paths and encryption. It also offers an abstraction layer through the `ILocalPrefs` interface to enable unified save/load management.
3. It supports high-performance serialization using either `System.Text.Json` or [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp).
4. By integrating with native JavaScript, it supports APIs for saving/loading using Local Storage and IndexedDB, along with unified implementations based on these.

## Installation
### NuGet Packages
LocalPrefs requires .NET Standard 2.1 or higher. (Currently in preparation.)

### .NET CLI
Coming soon.

### Package Manager
Coming soon.

### Unity
See the [Unity](#unity-1) section below for details.

## Quick Start
The simplest implementation uses `LocalPrefs.Shared`.
The types that can be saved/loaded depend on the serializer used. As long as the serializer’s requirements are met, any type can be handled.

```csharp
using AndanteTribe.IO;

var hoge = new Hoge();

// Save
await LocalPrefs.Shared.SaveAsync("hogeKey", hoge);

// HasKey
bool hasHoge = LocalPrefs.Shared.HasKey("hogeKey");

// Load
var hoge2 = LocalPrefs.Shared.Load<Hoge>("hogeKey");

// Delete
await LocalPrefs.Shared.DeleteAsync("hogeKey");

// DeleteAll
await LocalPrefs.Shared.DeleteAllAsync();
```

If you want to define a custom save path, you’ll need to instantiate the prefs yourself.
In that case, using the `ILocalPrefs` interface as an abstraction layer is recommended.

```csharp
using AndanteTribe.IO;
using AndanteTribe.IO.Json;

string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DefaultCompany", "test", "localprefs-test");

// System.Text.Json
ILocalPrefs jsonPrefs = new JsonLocalPrefs(path);
```

```csharp
using AndanteTribe.IO;
using AndanteTribe.IO.MessagePack;

string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DefaultCompany", "test", "localprefs-test");

// MessagePack-CSharp
ILocalPrefs msgpackPrefs = new MessagePackLocalPrefs(path);
```

## FileAccessor
This is an abstraction layer for file I/O used in LocalPrefs.
You can subclass `FileAccessor` to implement custom file-handling logic.

The factory method `FileAccessor.Create(in string path)` provides a default implementation using `System.IO`.

## Encryption
`CryptoFileAccessor` is a general-purpose implementation that enables encrypted saving and decrypted loading.
It can be passed to a `JsonLocalPrefs` instance as shown below:

```csharp
using AndanteTribe.IO;
using AndanteTribe.IO.Json;

string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DefaultCompany", "test", "localprefs-test");

byte[] key = {
    0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
    0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
    0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
    0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
};

public static readonly byte[] iv = {
    0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
    0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30
};

// Set CryptoFileAccessor
ILocalPrefs prefs = new JsonLocalPrefs(new CryptoFileAccessor(path, key, iv));

// Save
await prefs.SaveAsync("intkey", 123);

// Load
int value = prefs.Load<int>("intkey");
```

## System.Text.Json
By using `LocalPrefs.Json`, you can perform local save/load operations based on `System.Text.Json`.
The `JsonLocalPrefs` class implements `ILocalPrefs` and provides the following constructors:

```csharp
public JsonLocalPrefs(in string savePath, JsonSerializerOptions? options = null);

public JsonLocalPrefs(FileAccessor fileAccessor, JsonSerializerOptions? options = null);
```

## MessagePack-CSharp
By using `LocalPrefs.MessagePack`, you can perform local save/load operations based on [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp).
The `MessagePackLocalPrefs` class implements `ILocalPrefs` and provides the following constructors:

```csharp
public MessagePackLocalPrefs(in string savePath, IFormatterResolver? resolver);

public MessagePackLocalPrefs(FileAccessor fileAccessor, IFormatterResolver? resolver);

public MessagePackLocalPrefs(in string savePath, MessagePackSerializerOptions? options = null);

public MessagePackLocalPrefs(FileAccessor fileAccessor, MessagePackSerializerOptions? options = null);
```

## Unity
LocalPrefs is available for use in Unity.
A Unity-specific extension package, `LocalPrefs.Unity`, is also provided.

### Requirements
- Unity 2022.3 or later

### Installation
1. Install [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).
2. Open `NuGet > Manage NuGet Packages` and install the `System.Text.Json` or `MessagePack-CSharp` package.
3. If you use `MessagePack-CSharp`, also install the `MessagePack.Unity` package.
   > Install `MessagePack.Unity` package by referencing the git URL. Open Package Manager window and press `Add Package from git URL...`, enter following path
   >
   > https://github.com/MessagePack-CSharp/MessagePack-CSharp.git?path=src/MessagePack.UnityClient/Assets/Scripts/MessagePack
   > [MessagePack-CSharp README.md](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#unity-support)
4. Open `Window > Package Manager`, select `[+] > Add package from git URL`, and enter the following URLs:
   > ### Core package
   > ```
   > https://github.com/AndanteTribe/LocalPrefs.git?path=bin/LocalPrefs.Core
   > ```
   > ### For System.Text.Json
   > ```
   > https://github.com/AndanteTribe/LocalPrefs.git?path=bin/LocalPrefs.Json
   > ```
   > ### For MessagePack-CSharp
   > ```
   > https://github.com/AndanteTribe/LocalPrefs.git?path=bin/LocalPrefs.MessagePack
   > ```
   > ### Unity extension package
   > ```
   > https://github.com/AndanteTribe/LocalPrefs.git?path=src/LocalPrefs.Unity/Packages/jp.andantetribe.localprefs
   > ```

> \[!CAUTION]
> Once NuGet support for LocalPrefs is complete, these complicated installation steps are expected to be simplified.

### Auto Configuration of Save Path for LocalPrefs.Shared
When `LocalPrefs.Unity` is included, `LocalPrefs.Shared` will automatically configure the save path at startup.
By default, `Application.persistentDataPath` is used for non-web platforms, and Local Storage for web platforms.

### Web Support
`LocalPrefs.Unity` supports browser-based local saving/loading via native JavaScript integration.

Both **IndexedDB** and **Local Storage** are supported as storage backends.
If the data is small, Local Storage performs faster than IndexedDB. However, for large data like screenshots, IndexedDB is more suitable.

* Use `IDBUtils` for IndexedDB
* Use `LSUtils` for Local Storage

For advanced usage, `IDBStream` and `LSStream` are also available, implementing the .NET stream decorator paradigm.

#### IDBUtils
```csharp
public static async ValueTask WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default);

public static async ValueTask WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);

public static async ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default);

public static async ValueTask<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);
```

#### LSUtils
```csharp
public static void WriteAllBytes(in string path, in ReadOnlySpan<byte> bytes);

public static void WriteAllText(in string path, in string contents);

public static void Delete(in string path);

public static byte[] ReadAllBytes(in string path);

public static string ReadAllText(in string path);
```

## License
This library is released under the MIT license.