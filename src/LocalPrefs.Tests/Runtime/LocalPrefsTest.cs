#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace AndanteTribe.IO.Tests
{
    public static class LocalPrefsTest
    {
#if (!UNITY_EDITOR && UNITY_WEBGL) || DOTNET_TEST
        public const string TestFilePath = "localprefs-test";
#else
        public static string TestFilePath => UnityEngine.Application.persistentDataPath + "/localprefs-test";
#endif
        public static readonly byte[] TestKey = {
            0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
            0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F, 0x10,
            0x11, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17, 0x18,
            0x19, 0x1A, 0x1B, 0x1C, 0x1D, 0x1E, 0x1F, 0x20
        };

        public static readonly byte[] TestIv = {
            0x21, 0x22, 0x23, 0x24, 0x25, 0x26, 0x27, 0x28,
            0x29, 0x2A, 0x2B, 0x2C, 0x2D, 0x2E, 0x2F, 0x30
        };

        public static async ValueTask SaveAndLoad_Int(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            await prefs.SaveAsync("intKey", 123);
            var value = prefs.Load<int>("intKey");
            Assert.That(value, Is.EqualTo(123));
        }

        public static async ValueTask SaveAndLoad_Int_OtherInstance(Func<ILocalPrefs> factory)
        {
            var prefs1 = factory();
            await prefs1.SaveAsync("intKey", 123);
            var prefs2 = factory();
            var value = prefs2.Load<int>("intKey");
            Assert.That(value, Is.EqualTo(123));
        }

        public static async ValueTask SaveAndLoad_String(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            await prefs.SaveAsync("strKey", "hello");
            var value = prefs.Load<string>("strKey");
            Assert.That(value, Is.EqualTo("hello"));
        }

        public static async ValueTask SaveAndLoad_String_OtherInstance(Func<ILocalPrefs> factory)
        {
            var prefs1 = factory();
            await prefs1.SaveAsync("strKey", "hello");
            var prefs2 = factory();
            var value = prefs2.Load<string>("strKey");
            Assert.That(value, Is.EqualTo("hello"));
        }

        public static async ValueTask SaveAndLoad_CustomType(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            var data = new CustomData { Id = 1, Name = "abc" };
            await prefs.SaveAsync("custom", data);
            var loaded = prefs.Load<CustomData>("custom");
            Assert.That(loaded, Is.EqualTo(data));
        }

        public static async ValueTask SaveAndLoad_CustomType_OtherInstance(Func<ILocalPrefs> factory)
        {
            var prefs1 = factory();
            var data = new CustomData { Id = 1, Name = "abc" };
            await prefs1.SaveAsync("custom", data);
            var prefs2 = factory();
            var loaded = prefs2.Load<CustomData>("custom");
            Assert.That(loaded, Is.EqualTo(data));
        }

        public static async ValueTask OverwriteValue(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            await prefs.SaveAsync("key", 1);
            await prefs.SaveAsync("key", 2);
            var value = prefs.Load<int>("key");
            Assert.That(value, Is.EqualTo(2));
        }

        public static async ValueTask OverwriteValue_OtherInstance(Func<ILocalPrefs> factory)
        {
            var prefs1 = factory();
            await prefs1.SaveAsync("key", 1);
            var prefs2 = factory();
            await prefs2.SaveAsync("key", 2);
            var prefs3 = factory();
            var value = prefs3.Load<int>("key");
            Assert.That(value, Is.EqualTo(2));
        }

        public static async ValueTask HasKey_Works(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            Assert.That(prefs.HasKey("k"), Is.False);
            await prefs.SaveAsync("k", 42);
            Assert.That(prefs.HasKey("k"), Is.True);
        }

        public static async ValueTask HasKey_Works_OtherInstance(Func<ILocalPrefs> factory)
        {
            var prefs1 = factory();
            Assert.That(prefs1.HasKey("k"), Is.False);
            await prefs1.SaveAsync("k", 42);
            var prefs2 = factory();
            Assert.That(prefs2.HasKey("k"), Is.True);
        }

        public static async ValueTask Delete_RemovesKey(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            await prefs.SaveAsync("del", 99);
            Assert.That(prefs.HasKey("del"), Is.True);
            await prefs.DeleteAsync("del");
            Assert.That(prefs.HasKey("del"), Is.False);
            Assert.That(prefs.Load<int>("del"), Is.EqualTo(0));
        }

        public static async ValueTask Delete_RemovesKey_OtherInstance(Func<ILocalPrefs> factory)
        {
            var prefs1 = factory();
            await prefs1.SaveAsync("del", 99);
            Assert.That(prefs1.HasKey("del"), Is.True);
            var prefs2 = factory();
            await prefs2.DeleteAsync("del");
            var prefs3 = factory();
            Assert.That(prefs3.HasKey("del"), Is.False);
            Assert.That(prefs3.Load<int>("del"), Is.EqualTo(0));
        }

        public static async ValueTask Delete_EmptyPrefs_Throws(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            try
            {
                await prefs.DeleteAsync("notfound");
            }
            catch (KeyNotFoundException)
            {
                Assert.That(true);
                return;
            }
            Assert.That(false);
        }

        public static async ValueTask DeleteAll_RemovesAll(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            await prefs.SaveAsync("a", 1);
            await prefs.SaveAsync("b", 2);
            await prefs.DeleteAllAsync();
            Assert.That(prefs.HasKey("a"), Is.False);
            Assert.That(prefs.HasKey("b"), Is.False);
        }

        public static async ValueTask DeleteAll_RemovesAll_OtherInstance(Func<ILocalPrefs> factory)
        {
            var prefs1 = factory();
            await prefs1.SaveAsync("a", 1);
            await prefs1.SaveAsync("b", 2);
            var prefs2 = factory();
            await prefs2.DeleteAllAsync();
            var prefs3 = factory();
            Assert.That(prefs3.HasKey("a"), Is.False);
            Assert.That(prefs3.HasKey("b"), Is.False);
        }

        public static void Load_NonExistentKey_ReturnsDefault(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            var value = prefs.Load<string>("none");
            Assert.That(value, Is.Null);
        }

        public static async ValueTask Delete_NonExistentKey_Throws(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            try
            {
                await prefs.DeleteAsync("notfound");
            }
            catch (KeyNotFoundException)
            {
                Assert.That(true);
                return;
            }
            Assert.That(false);
        }

        public static async ValueTask Delete_SecondElement(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            await prefs.SaveAsync("a", 1);
            await prefs.SaveAsync("b", 2);
            await prefs.SaveAsync("c", 3);
            await prefs.DeleteAsync("b");

            Assert.That(prefs.Load<int>("a"), Is.EqualTo(1));
            Assert.That(prefs.Load<int>("b"), Is.EqualTo(0));
            Assert.That(prefs.Load<int>("c"), Is.EqualTo(3));
        }

        public static async ValueTask AddAndRemoveMultipleTimes(Func<ILocalPrefs> factory)
        {
            var prefs = factory();
            for (int i = 0; i < 10; i++)
            {
                await prefs.SaveAsync($"key{i}", i);
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.That(prefs.Load<int>($"key{i}"), Is.EqualTo(i));
                await prefs.DeleteAsync($"key{i}");
            }

            for (int i = 0; i < 10; i++)
            {
                Assert.That(prefs.Load<int>($"key{i}"), Is.EqualTo(0));
            }
        }
    }
}