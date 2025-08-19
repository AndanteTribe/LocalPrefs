# LocalPrefs
[![dotnet-test](https://github.com/AndanteTribe/LocalPrefs/actions/workflows/dotnet-test.yml/badge.svg)](https://github.com/AndanteTribe/LocalPrefs/actions/workflows/dotnet-test.yml)
[![unity-test](https://github.com/AndanteTribe/LocalPrefs/actions/workflows/unity-test.yml/badge.svg)](https://github.com/AndanteTribe/LocalPrefs/actions/workflows/unity-test.yml)
[![nuget-core](https://img.shields.io/nuget/v/LocalPrefs.Core.svg)](https://www.nuget.org/packages/LocalPrefs.Core/)
[![nuget-json](https://img.shields.io/nuget/v/LocalPrefs.Json.svg)](https://www.nuget.org/packages/LocalPrefs.Json/)
[![nuget-msgpack](https://img.shields.io/nuget/v/LocalPrefs.MessagePack.svg)](https://www.nuget.org/packages/LocalPrefs.MessagePack/)
[![Releases](https://img.shields.io/github/release/AndanteTribe/LocalPrefs.svg)](https://github.com/AndanteTribe/LocalPrefs/releases)
[![GitHub license](https://img.shields.io/github/license/AndanteTribe/LocalPrefs.svg)](./LICENSE)
[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/AndanteTribe/LocalPrefs)

[English]((./README.md)) | 日本語

## 概要
**LocalPrefs** は .NET / Unity向けにローカルセーブ・ロードの機能を提供するライブラリです。

Unityの `UnityEngine.PlayerPrefs` は非常に問題の多いAPIで、以下のような欠点があります。

1. Windowsプラットフォームではレジストリに保存される。
2. WebプラットフォームではIndexedDBに保存されるが、保存先のKeyには不可解なハッシュが混入し、このハッシュは特定の条件下でビルド毎に変わる。この仕様のために、ゲームの更新に支障があったり、異なるハッシュのKeyごとに保存されるためIndexedDBのローカル保存が汚染されたりする場合がある。
3. Webプラットフォームで利用する場合、1MBという制限がある。
4. Webプラットフォームでは即座に保存されず、終了タイミングがわからない非同期APIとなる。
5. APIとして対応する型が限定されている。（int型, float型, string型のみ）

LocalPrefsはこれらの問題を解消し、かつ高速な実装を提供します。

1. `LocalPrefs.Shared` の保存先はUnityの場合は `Application.persistentDataPath`、.NETの場合もそれに準じたパスが指定されます。
2. 保存先や暗号化など、独自の拡張をすることを入れ込むことを可能にするAPIを提供します。また、セーブ・ロードの一括制御をする実装の抽象レイヤーとして `ILocalPrefs` インターフェイスを提供します。
3. `System.Text.Json` または [MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp) を利用した、高速な読み込み実装を提供します。
4. JavaScriptネイティブな実装との連携により、Local storageとIndexedDBへの保存・展開を可能とするAPIの提供、およびそれらを利用した一括制御実装を提供します。
## インストール
### NuGet packages
LocalPrefsを利用するには .NET Standard2.1 以上が必要です。パッケージはNuGetから入手できます。
### .NET CLI
#### LocalPrefs.Json(System.Text.Json利用)
```ps1
dotnet add package LocalPrefs.Json
```

#### LocalPrefs.MessagePack(MessagePack-CSharp利用)
```ps1
dotnet add package LocalPrefs.MessagePack
```

### Package Manager
#### LocalPrefs.Json(System.Text.Json利用)
```ps1
Install-Package LocalPrefs.Json
```

#### LocalPrefs.MessagePack(MessagePack-CSharp利用)
```ps1
Install-Package LocalPrefs.MessagePack
```

### Unity
詳細は[Unity](#unity-1)の項目を参照してください。
## クイックスタート
`LocalPrefs.Shared` を用いた実装が最も簡易的です。
指定する型は依存するシリアライザーによります。シリアライザー側の要件を満たせればどの型でもセーブ・ロードが可能です。

```cs
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

ファイルのパスを任意に定めたい場合は、利用者側でインスタンスを作成する必要があります。
その場合、`ILocalPrefs` インターフェイスを抽象レイヤーとして利用することを推奨します。

```cs
using AndanteTribe.IO;
using AndanteTribe.IO.Json;

string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DefaultCompany", "test", "localprefs-test");

// System.Text.Json
ILocalPrefs jsonPrefs = new JsonLocalPrefs(path);
```

```cs
using AndanteTribe.IO;
using AndanteTribe.IO.MessagePack;

string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DefaultCompany", "test", "localprefs-test");

// MessagePack-CSharp
ILocalPrefs msgpackPrefs = new MessagePackLocalPrefs(path);
```
## FileAccessor
LocalPrefsでのファイルの入出力操作実装の抽象レイヤーです。
`FileAccessor` クラスを継承したクラスを利用者側で実装し、ファイル操作周りに独自の処理を入れ込むことができます。

ファクトリメソッド `FileAccessor.Create(in string path)` で、 `System.IO` を利用した標準的なファイル操作を実装済みの `FileAccessor` インスタンスを作成することができます。
## 暗号化
`FileAccessor` 実装の一例である `CryptoFileAccessor` を、暗号化セーブ・複合化ロードの汎用実装として提供しています。

```cs
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
`LocalPrefs.Json` を使用すると、`System.Text.Json` を基盤にしたローカルセーブ・ロードを利用することができます。
`JsonLocalPrefs` クラスは `ILocalPrefs` を実装し、以下のようなコンストラクタがあります。

```cs
public JsonLocalPrefs(in string savePath, JsonSerializerOptions? options = null);

public JsonLocalPrefs(FileAccessor fileAccessor, JsonSerializerOptions? options = null);
```
## MessagePack-CSharp
`LocalPrefs.MessagePack` を使用すると、[MessagePack-CSharp](https://github.com/MessagePack-CSharp/MessagePack-CSharp) を基盤にしたローカルセーブ・ロードを利用することができます。
`MessagePackLocalPrefs` クラスは `ILocalPrefs` を実装し、以下のようなコンストラクタがあります。

```cs
public MessagePackLocalPrefs(in string savePath, IFormatterResolver? resolver);

public MessagePackLocalPrefs(FileAccessor fileAccessor, IFormatterResolver? resolver);

public MessagePackLocalPrefs(in string savePath, MessagePackSerializerOptions? options = null);

public MessagePackLocalPrefs(FileAccessor fileAccessor, MessagePackSerializerOptions? options = null);
```
## Unity
LocalPrefsはUnityで使用可能です。
またUnity向けの拡張パッケージである `LocalPrefs.Unity` も提供しています。
### 要件
- Unity 2022.3以上
### インストール
1. [NugetForUnity](https://github.com/GlitchEnzo/NuGetForUnity)をインストールします。
2.  `NuGet > Manage NuGet Packages` からNuGetウィンドウを開き、`LocalPrefs.Json` または `LocalPrefs.MessagePack` パッケージを検索してインストールします。
3. `LocalPrefs.MessagePack`(`MessagePack-CSharp`依存) を利用する場合は、`MessagePack.Unity` パッケージもインストールします。
   > Install `MessagePack.Unity` package by referencing the git URL. Open Package Manager window and press `Add Package from git URL...`, enter following path
   >
   > https://github.com/MessagePack-CSharp/MessagePack-CSharp.git?path=src/MessagePack.UnityClient/Assets/Scripts/MessagePack
   > [MessagePack-CSharp README.md](https://github.com/MessagePack-CSharp/MessagePack-CSharp?tab=readme-ov-file#unity-support)
4. `Window > Package Manager` からPackage Managerウィンドウを開き、`[+] > Add package from git URL` を選択して以下のURLを入力します。
    ```
   https://github.com/AndanteTribe/LocalPrefs.git?path=src/LocalPrefs.Unity/Packages/jp.andantetribe.localprefs
    ```

### LocalPrefs.Sharedの保存先の自動設定
`LocalPrefs.Unity` を導入した場合、`LocalPrefs.Shared` はデフォルトで起動時に自動的に保存先を適切なものに設定されます。
具体的には、Web以外であれば `Application.persistentDataPath`、WebではLocal storageになります。
### Webサポート
`LocalPrefs.Unity` ではJavaScriptネイティブな実装と連携した、ブラウザでのローカルセーブ・ロードをサポートします。

保存先には IndexedDB と Local storage の2つをサポートしています。
データが軽量であれば Local storage は IndexedDB より高速に機能しますが、ゲーム内のスクリーンショットなどの大きなデータを扱う場合は IndexedDB が便利です。

IndexedDB は `IDBUtils` に、Local storage は `LSUtils` にそれぞれネイティブ実装を呼び出す直接的なAPIがあります。
また、`IDBUtils` を基盤にした `IDBStream`、`LSUtils` を基盤にした `LSStream` も提供しており、これらは .NET のStream デコレータパターンパラダイムに組み込む形での利用を検討可能です。
#### IDBUtils
```cs
public static async ValueTask WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default);

public static async ValueTask WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);

public static async ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default);

public static async ValueTask<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);
```
#### LSUtils
```cs
public static void WriteAllBytes(in string path, in ReadOnlySpan<byte> bytes);

public static void WriteAllText(in string path, in string contents);

public static void Delete(in string path);

public static byte[] ReadAllBytes(in string path);

public static string ReadAllText(in string path);
```
### ライセンス
このライブラリは、MITライセンスで公開しています。
