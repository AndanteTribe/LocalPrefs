#if UNITY_WEBGL
#nullable enable

using System;
using System.Collections;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace AndanteTribe.IO.Unity.Tests
{
    public class IDBUtilsTest
    {
        private static readonly Type s_valueTaskSourceType = typeof(IDBUtils).Assembly.GetType(
            "AndanteTribe.IO.Unity.IDBValueTaskSource")!;

        [Test]
        public void NativeCompletionDoesNotReturnSourceToPoolBeforeCallbackFinishes()
        {
            var source = CreateValueTaskSource();
            var valueTaskSource = (IValueTaskSource)source;
            var version = GetVersion(source);
            object? sourceCreatedFromContinuation = null;

            valueTaskSource.OnCompleted(
                _ =>
                {
                    valueTaskSource.GetResult(version);
                    sourceCreatedFromContinuation = CreateValueTaskSource();
                },
                null,
                version,
                ValueTaskSourceOnCompletedFlags.None);

            SetResult(source);

            Assert.That(sourceCreatedFromContinuation, Is.Not.Null);
            try
            {
                Assert.That(sourceCreatedFromContinuation, Is.Not.SameAs(source));
            }
            finally
            {
                if (sourceCreatedFromContinuation != null && !ReferenceEquals(sourceCreatedFromContinuation, source))
                {
                    CompleteAndConsume(sourceCreatedFromContinuation);
                }
            }
        }

        [UnityTest]
        public IEnumerator CanceledOperationDoesNotCorruptFollowingOperation()
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
                await Awaitable.WaitForSecondsAsync(0.1f);

                var expected = new byte[] { 1, 2, 3, 4 };
                await IDBUtils.WriteAllBytesAsync(path, expected);
                var actual = await IDBUtils.ReadAllBytesAsync(path);
                Assert.That(actual, Is.EqualTo(expected));
                await IDBUtils.DeleteAsync(path);
            });
        }

        private static object CreateValueTaskSource()
            => s_valueTaskSourceType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, null)!;

        private static short GetVersion(object source)
            => (short)s_valueTaskSourceType.GetProperty("Version")!.GetValue(source)!;

        private static void SetResult(object source)
            => s_valueTaskSourceType.GetMethod("SetResult", Type.EmptyTypes)!.Invoke(source, null);

        private static void CompleteAndConsume(object source)
        {
            var version = GetVersion(source);
            SetResult(source);
            ((IValueTaskSource)source).GetResult(version);
        }
    }
}

#endif
