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
        private static readonly List<EventID> s_ids = new();

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
            var source = IDBValueTaskSourcePool.Shared.Get();
            var eventID = EventID.GetNext(source);

            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(() => CancelEventInternal(eventID));

            s_ids.Add(eventID);
            SaveToIndexedDB((uint)eventID, path, bytes, bytes.Length, static u => NonLoadSuccessCallback(u), static (u, s) => ErrorCallback(u, s));

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
            var source = IDBValueTaskSourcePool.Shared.Get();
            var eventID = EventID.GetNext(source);

            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(() => CancelEventInternal(eventID));

            s_ids.Add(eventID);

            unsafe
            {
                fixed (byte* dataPtr = bytes.Span)
                {
                    SaveToIndexedDB((uint)eventID, path, new IntPtr(dataPtr), bytes.Length, static u => NonLoadSuccessCallback(u), static (u, s) => ErrorCallback(u, s));
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
            var source = IDBValueTaskSourcePool.Shared.Get();
            var eventID = EventID.GetNext(source);

            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(() => CancelEventInternal(eventID));

            s_ids.Add(eventID);
            DeleteFromIndexedDB((uint)eventID, path, static u => NonLoadSuccessCallback(u), static (u, s) => ErrorCallback(u, s));

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
            var source = IDBValueTaskSourcePool.Shared.Get();
            var eventID = EventID.GetNext(source);

            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(() => CancelEventInternal(eventID));

            ReadAllBytesInternal(path, EventID.GetNext(source));
            return (await new ValueTask<(byte[] array, int _)>(source, source.Version)).array;
        }

        internal static void ReadAllBytesInternal(in string key, in EventID eventID)
        {
            s_ids.Add(eventID);
            LoadFromIndexedDB((uint)eventID, key, static (u, ptr, i) => LoadSuccessCallback(u, ptr, i), static (u, s) => ErrorCallback(u, s));
        }

        internal static void CancelEventInternal(in EventID eventID)
        {
            eventID.Source.SetCanceled();
            s_ids.Remove(eventID);
        }

        [DllImport("__Internal")]
        private static extern void SaveToIndexedDB(uint id, string key, byte[] data, int dataSize, Action<uint> success, Action<uint, string> error);

        [DllImport("__Internal")]
        private static extern void SaveToIndexedDB(uint id, string key, IntPtr data, int dataSize, Action<uint> success, Action<uint, string> error);

        [DllImport("__Internal")]
        private static extern void DeleteFromIndexedDB(uint id, string key, Action<uint> success, Action<uint, string> error);

        [DllImport("__Internal")]
        private static extern void LoadFromIndexedDB(uint id, string key, Action<uint, IntPtr, int> success, Action<uint, string> error);

        [MonoPInvokeCallback(typeof(Action<uint>))]
        private static void NonLoadSuccessCallback(uint id)
        {
            var eventID = s_ids[s_ids.BinarySearch((EventID)id)];
            eventID.Source.SetResult();
            s_ids.Remove(eventID);
        }

        [MonoPInvokeCallback(typeof(Action<uint, IntPtr, int>))]
        private static void LoadSuccessCallback(uint id, IntPtr dataPtr, int length)
        {
            var eventID = s_ids[s_ids.BinarySearch((EventID)id)];
            eventID.Source.SetResult(dataPtr, length);
            s_ids.Remove(eventID);
        }

        [MonoPInvokeCallback(typeof(Action<uint, string>))]
        private static void ErrorCallback(uint id, string message)
        {
            var eventID = s_ids[s_ids.BinarySearch((EventID)id)];
            eventID.Source.SetException(new Exception(message));
            s_ids.Remove(eventID);
        }
    }
}

#endif