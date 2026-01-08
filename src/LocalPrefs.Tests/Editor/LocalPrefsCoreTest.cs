#nullable enable

using System;
using System.IO;
using System.Threading.Tasks;
using AndanteTribe.IO.Json;
using AndanteTribe.IO.MessagePack;
using NUnit.Framework;

namespace AndanteTribe.IO.Tests
{
    public class LocalPrefsCoreTest
    {
        private static readonly Func<ILocalPrefs>[] s_factories =
        {
            () => new JsonLocalPrefs(LocalPrefsTest.TestFilePath),
            () => new MessagePackLocalPrefs(LocalPrefsTest.TestFilePath),
            () => new JsonLocalPrefs(CreateCryptoFileAccessor(LocalPrefsTest.TestFilePath, LocalPrefsTest.TestKey, LocalPrefsTest.TestIv)),
            () => new MessagePackLocalPrefs(CreateCryptoFileAccessor(LocalPrefsTest.TestFilePath, LocalPrefsTest.TestKey, LocalPrefsTest.TestIv)),
            () => new JsonLocalPrefs(CreateCryptoFileAccessor(LocalPrefsTest.TestFilePath, LocalPrefsTest.TestKey)),
            () => new MessagePackLocalPrefs(CreateCryptoFileAccessor(LocalPrefsTest.TestFilePath, LocalPrefsTest.TestKey)),
        };

        private static CryptoFileAccessor CreateCryptoFileAccessor(string path, byte[] key, byte[]? iv = null)
        {
            var aes = System.Security.Cryptography.Aes.Create();
            aes.Key = key;
            if (iv != null && iv.Length > 0)
            {
                aes.IV = iv;
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
            }
            else
            {
                aes.Mode = System.Security.Cryptography.CipherMode.ECB;
            }
            
            var encryptor = aes.CreateEncryptor();
            var decryptor = aes.CreateDecryptor();
            aes.Dispose();
            
            return new CryptoFileAccessor(path, encryptor, decryptor);
        }

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            // 通常のテストファイルを削除
            if (File.Exists(LocalPrefsTest.TestFilePath))
            {
                File.Delete(LocalPrefsTest.TestFilePath);
            }

            // 暗号化されたテストファイルも削除
            if (File.Exists(LocalPrefsTest.TestFilePath + ".crypto.cbc"))
            {
                File.Delete(LocalPrefsTest.TestFilePath + ".crypto.cbc");
            }

            if (File.Exists(LocalPrefsTest.TestFilePath + ".crypto.ecb"))
            {
                File.Delete(LocalPrefsTest.TestFilePath + ".crypto.ecb");
            }
        }

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_Int(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_Int(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_Int_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_Int_OtherInstance(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_String(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_String(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_String_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_String_OtherInstance(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_CustomType(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_CustomType(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_CustomType_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_CustomType_OtherInstance(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task OverwriteValue(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.OverwriteValue(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task OverwriteValue_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.OverwriteValue_OtherInstance(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task HasKey_Works(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.HasKey_Works(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task HasKey_Works_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.HasKey_Works_OtherInstance(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_RemovesKey(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_RemovesKey(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_RemovesKey_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_RemovesKey_OtherInstance(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_EmptyPrefs_Throws(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_EmptyPrefs_Throws(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task DeleteAll_RemovesAll(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.DeleteAll_RemovesAll(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task DeleteAll_RemovesAll_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.DeleteAll_RemovesAll_OtherInstance(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public void Load_NonExistentKey_ReturnsDefault(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Load_NonExistentKey_ReturnsDefault(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_NonExistentKey_Throws(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_NonExistentKey_Throws(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_SecondElement(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_SecondElement(factory).AsTask();

        [TestCaseSource(nameof(s_factories))]
        public Task AddAndRemoveMultipleTimes(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.AddAndRemoveMultipleTimes(factory).AsTask();
    }
}