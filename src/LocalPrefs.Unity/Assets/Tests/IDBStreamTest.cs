#if UNITY_WEBGL
#nullable enable

using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AndanteTribe.IO.Unity.Tests
{
    public class IDBStreamTest
    {
        [UnityTest]
        public IEnumerator MultipleWrites_ArePersistedOnDisposeAsync()
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                var path = $"idb-stream-buffered-write-{Guid.NewGuid():N}";
                await using (var stream = new IDBStream(path))
                {
                    await stream.WriteAsync(new byte[] { 1, 2 });
                    await stream.WriteAsync(new byte[] { 3, 4 });
                }

                var actual = await IDBUtils.ReadAllBytesAsync(path);
                Assert.That(actual, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
                await IDBUtils.DeleteAsync(path);
            });
        }
    }
}

#endif