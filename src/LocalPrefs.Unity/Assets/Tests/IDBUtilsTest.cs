#if UNITY_WEBGL
#nullable enable

using System;
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace AndanteTribe.IO.Unity.Tests
{
    public class IDBUtilsTest
    {
        [UnityTest]
        public IEnumerator CanceledOperation_DoesNotCorruptFollowingOperation()
        {
            yield return new ToCoroutineEnumerator(async () =>
            {
                var path = $"idb-cancellation-{Guid.NewGuid():N}";
                using var cancellationSource = new CancellationTokenSource();
                var canceledOperation = IDBUtils.ReadAllBytesAsync(path, cancellationSource.Token);
                cancellationSource.Cancel();

                try
                {
                    await canceledOperation;
                    Assert.Fail("The IndexedDB operation was expected to be canceled.");
                }
                catch (TaskCanceledException)
                {
                }

                // Allow the canceled browser request to deliver its terminal callback before the pooled source is reused.
                await Task.Delay(100);

                var expected = new byte[] { 1, 2, 3, 4 };
                await IDBUtils.WriteAllBytesAsync(path, expected);
                var actual = await IDBUtils.ReadAllBytesAsync(path);
                Assert.That(actual, Is.EqualTo(expected));
                await IDBUtils.DeleteAsync(path);
            });
        }
    }
}

#endif
