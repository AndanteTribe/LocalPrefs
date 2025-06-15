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
        };

        [SetUp]
        public void Setup()
        {
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(LocalPrefsTest.TestFilePath))
            {
                File.Delete(LocalPrefsTest.TestFilePath);
            }
        }

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_Int(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_Int(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_Int_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_Int_OtherInstance(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_String(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_String(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_String_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_String_OtherInstance(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_CustomType(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_CustomType(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task SaveAndLoad_CustomType_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.SaveAndLoad_CustomType_OtherInstance(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task OverwriteValue(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.OverwriteValue(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task OverwriteValue_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.OverwriteValue_OtherInstance(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task HasKey_Works(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.HasKey_Works(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task HasKey_Works_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.HasKey_Works_OtherInstance(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_RemovesKey(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_RemovesKey(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_RemovesKey_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_RemovesKey_OtherInstance(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_EmptyPrefs_Throws(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_EmptyPrefs_Throws(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task DeleteAll_RemovesAll(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.DeleteAll_RemovesAll(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task DeleteAll_RemovesAll_OtherInstance(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.DeleteAll_RemovesAll_OtherInstance(factory);

        [TestCaseSource(nameof(s_factories))]
        public void Load_NonExistentKey_ReturnsDefault(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Load_NonExistentKey_ReturnsDefault(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_NonExistentKey_Throws(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_NonExistentKey_Throws(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task Delete_SecondElement(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.Delete_SecondElement(factory);

        [TestCaseSource(nameof(s_factories))]
        public Task AddAndRemoveMultipleTimes(Func<ILocalPrefs> factory)=>
            LocalPrefsTest.AddAndRemoveMultipleTimes(factory);
    }
}