#if UNITY_WEBGL
#nullable enable

using System;
using System.Collections;
using AndanteTribe.IO.Json;
using AndanteTribe.IO.MessagePack;
using AndanteTribe.IO.Tests;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AndanteTribe.IO.Unity.Tests
{
    public class LSPrefsTest
    {
        private static readonly LSAccessor s_accessor = new(LocalPrefsTest.TestFilePath);

        private static readonly Func<ILocalPrefs>[] s_factories =
        {
            () => new JsonLocalPrefs(s_accessor),
            () => new MessagePackLocalPrefs(s_accessor),
            () => new JsonLocalPrefs(CreateCryptoFileAccessor(s_accessor, LocalPrefsTest.TestKey, LocalPrefsTest.TestIv)),
            () => new MessagePackLocalPrefs(CreateCryptoFileAccessor(s_accessor, LocalPrefsTest.TestKey, LocalPrefsTest.TestIv)),
            () => new JsonLocalPrefs(CreateCryptoFileAccessor(s_accessor, LocalPrefsTest.TestKey)),
            () => new MessagePackLocalPrefs(CreateCryptoFileAccessor(s_accessor, LocalPrefsTest.TestKey)),
        };

        private static CryptoFileAccessor CreateCryptoFileAccessor(FileAccessor fileAccessor, byte[] key, byte[]? iv = null)
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
            
            return new CryptoFileAccessor(fileAccessor, encryptor, decryptor);
        }

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            LSUtils.Delete(LocalPrefsTest.TestFilePath);
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_Int([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.SaveAndLoad_Int(factory);
            });
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_Int_OtherInstance([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.SaveAndLoad_Int_OtherInstance(factory);
            });
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_String([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.SaveAndLoad_String(factory);
            });
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_String_OtherInstance([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.SaveAndLoad_String_OtherInstance(factory);
            });
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_CustomType([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.SaveAndLoad_CustomType(factory);
            });
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_CustomType_OtherInstance([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.SaveAndLoad_CustomType_OtherInstance(factory);
            });
        }

        [UnityTest]
        public IEnumerator OverwriteValue([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.OverwriteValue(factory);
            });
        }

        [UnityTest]
        public IEnumerator OverwriteValue_OtherInstance([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.OverwriteValue_OtherInstance(factory);
            });
        }

        [UnityTest]
        public IEnumerator HasKey_Works([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.HasKey_Works(factory);
            });
        }

        [UnityTest]
        public IEnumerator HasKey_Works_OtherInstance([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.HasKey_Works_OtherInstance(factory);
            });
        }

        [UnityTest]
        public IEnumerator Delete_RemovesKey([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.Delete_RemovesKey(factory);
            });
        }

        [UnityTest]
        public IEnumerator Delete_RemovesKey_OtherInstance([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.Delete_RemovesKey_OtherInstance(factory);
            });
        }

        [UnityTest]
        public IEnumerator Delete_EmptyPrefs_Throws([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.Delete_EmptyPrefs_Throws(factory);
            });
        }

        [UnityTest]
        public IEnumerator DeleteAll_RemovesAll([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.DeleteAll_RemovesAll(factory);
            });
        }

        [UnityTest]
        public IEnumerator DeleteAll_RemovesAll_OtherInstance([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.DeleteAll_RemovesAll_OtherInstance(factory);
            });
        }

        [Test]
        public void Load_NonExistentKey_ReturnsDefault([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory) =>
            LocalPrefsTest.Load_NonExistentKey_ReturnsDefault(factory);

        [UnityTest]
        public IEnumerator Delete_NonExistentKey_Throws([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.Delete_NonExistentKey_Throws(factory);
            });
        }

        [UnityTest]
        public IEnumerator Delete_SecondElement([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.Delete_SecondElement(factory);
            });
        }

        [UnityTest]
        public IEnumerator AddAndRemoveMultipleTimes([ValueSource(nameof(s_factories))] Func<ILocalPrefs> factory)
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                await LocalPrefsTest.AddAndRemoveMultipleTimes(factory);
            });
        }
    }
}

#endif