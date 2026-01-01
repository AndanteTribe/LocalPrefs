#if UNITY_WEBGL
#nullable enable

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AOT;

namespace AndanteTribe.IO.Unity
{
    /// <summary>
    /// Provides utility methods for interacting with IndexedDB in a WebGL environment.
    /// </summary>
    public static class IDBUtils
    {
        /// <summary>
        /// Asynchronously writes the specified byte array to IndexedDB using the specified path as key.
        /// If the path already exists in IndexedDB, it is overwritten.
        /// </summary>
        /// <param name="path">The path string that serves as the key.</param>
        /// <param name="bytes">The bytes to write to IndexedDB.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static async ValueTask WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = IDBValueTaskSource.Create();
            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(static s =>
            {
                ((IDBValueTaskSource)s).SetCanceled();
            }, source);

            SaveToIndexedDB(source.Handle, path, bytes, bytes.Length, NonLoadSuccessCallback, ErrorCallback);
            await new ValueTask(source, source.Version);
        }

        /// <summary>
        /// Asynchronously writes the specified byte array to IndexedDB using the specified path as key.
        /// If the path already exists in IndexedDB, it is overwritten.
        /// </summary>
        /// <param name="path">The path string that serves as the key.</param>
        /// <param name="bytes"> The bytes to write to IndexedDB.</param>
        /// <param name="cancellationToken"> The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous write operation.</returns>
        public static async ValueTask WriteAllBytesAsync(string path, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = IDBValueTaskSource.Create();
            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(static s =>
            {
                ((IDBValueTaskSource)s).SetCanceled();
            }, source);

            unsafe
            {
                fixed (byte* dataPtr = bytes.Span)
                {
                    SaveToIndexedDB(source.Handle, path, new IntPtr(dataPtr), bytes.Length, NonLoadSuccessCallback, ErrorCallback);
                }
            }

            await new ValueTask(source, source.Version);
        }

        /// <summary>
        /// Asynchronously deletes the specified path from IndexedDB.
        /// </summary>
        /// <param name="path"> The path string that serves as the key to delete.</param>
        /// <param name="cancellationToken"> The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous delete operation.</returns>
        public static async ValueTask DeleteAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = IDBValueTaskSource.Create();
            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(static s =>
            {
                ((IDBValueTaskSource)s).SetCanceled();
            }, source);

            DeleteFromIndexedDB(source.Handle, path, NonLoadSuccessCallback, ErrorCallback);
            await new ValueTask(source, source.Version);
        }

        /// <summary>
        /// Asynchronously reads all bytes from IndexedDB using the specified path as key.
        /// </summary>
        /// <param name="path"> The path string that serves as the key.</param>
        /// <param name="cancellationToken"> The token to monitor for cancellation requests. The default value is None.</param>
        /// <returns>A task that represents the asynchronous read operation, containing the byte array read from IndexedDB.</returns>
        public static async ValueTask<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = IDBValueTaskSource.Create();
            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(static s =>
            {
                ((IDBValueTaskSource)s).SetCanceled();
            }, source);

            LoadFromIndexedDB(source.Handle, path, LoadSuccessCallback, ErrorCallback);
            return (await new ValueTask<(byte[] array, int _)>(source, source.Version)).array;
        }

        [DllImport("__Internal")]
        private static extern void SaveToIndexedDB(IntPtr state, string key, byte[] data, int dataSize, Action<IntPtr> success, Action<IntPtr, string> error);

        [DllImport("__Internal")]
        private static extern void SaveToIndexedDB(IntPtr state, string key, IntPtr data, int dataSize, Action<IntPtr> success, Action<IntPtr, string> error);

        [DllImport("__Internal")]
        private static extern void DeleteFromIndexedDB(IntPtr state, string key, Action<IntPtr> success, Action<IntPtr, string> error);

        [DllImport("__Internal")]
        internal static extern void LoadFromIndexedDB(IntPtr state, string key, Action<IntPtr, IntPtr, int> success, Action<IntPtr, string> error);

        [MonoPInvokeCallback(typeof(Action<IntPtr>))]
        private static void NonLoadSuccessCallback(IntPtr state)
        {
            var handle = GCHandle.FromIntPtr(state);
            var source = (IDBValueTaskSource)handle.Target;
            source.SetResult();
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, IntPtr, int>))]
        internal static void LoadSuccessCallback(IntPtr state, IntPtr dataPtr, int length)
        {
            var handle = GCHandle.FromIntPtr(state);
            var source = (IDBValueTaskSource)handle.Target;
            source.SetResult(dataPtr, length);
        }

        [MonoPInvokeCallback(typeof(Action<IntPtr, string>))]
        internal static void ErrorCallback(IntPtr state, string message)
        {
            var handle = GCHandle.FromIntPtr(state);
            var source = (IDBValueTaskSource)handle.Target;
            source.SetException(new Exception(message));
        }
    }
}

#endif