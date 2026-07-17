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
        private static readonly NonLoadSuccessCallbackDelegate s_nonLoadSuccessCallback = NonLoadSuccessCallback;
        private static readonly LoadSuccessCallbackDelegate s_loadSuccessCallback = LoadSuccessCallback;
        private static readonly ErrorCallbackDelegate s_errorCallback = ErrorCallback;
        private static readonly IntPtr s_nonLoadSuccessCallbackPointer = Marshal.GetFunctionPointerForDelegate(s_nonLoadSuccessCallback);
        private static readonly IntPtr s_loadSuccessCallbackPointer = Marshal.GetFunctionPointerForDelegate(s_loadSuccessCallback);
        private static readonly IntPtr s_errorCallbackPointer = Marshal.GetFunctionPointerForDelegate(s_errorCallback);

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

            LocalPrefsNativeInterop.SaveToIndexedDB(
                source.Handle,
                path,
                bytes,
                bytes.Length,
                s_nonLoadSuccessCallbackPointer,
                s_errorCallbackPointer);
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
                    LocalPrefsNativeInterop.SaveToIndexedDB(
                        source.Handle,
                        path,
                        new IntPtr(dataPtr),
                        bytes.Length,
                        s_nonLoadSuccessCallbackPointer,
                        s_errorCallbackPointer);
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

            LocalPrefsNativeInterop.DeleteFromIndexedDB(
                source.Handle,
                path,
                s_nonLoadSuccessCallbackPointer,
                s_errorCallbackPointer);
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

            LoadFromIndexedDB(source, path);
            return (await new ValueTask<(byte[] array, int _)>(source, source.Version)).array;
        }

        internal static void LoadFromIndexedDB(IDBValueTaskSource source, string path) =>
            LocalPrefsNativeInterop.LoadFromIndexedDB(
                source.Handle,
                path,
                s_loadSuccessCallbackPointer,
                s_errorCallbackPointer);

        [MonoPInvokeCallback(typeof(NonLoadSuccessCallbackDelegate))]
        private static void NonLoadSuccessCallback(IntPtr state)
        {
            var handle = GCHandle.FromIntPtr(state);
            var source = (IDBValueTaskSource)handle.Target;
            source.SetResult();
        }

        [MonoPInvokeCallback(typeof(LoadSuccessCallbackDelegate))]
        private static void LoadSuccessCallback(IntPtr state, IntPtr dataPtr, int length)
        {
            var handle = GCHandle.FromIntPtr(state);
            var source = (IDBValueTaskSource)handle.Target;
            source.SetResult(dataPtr, length);
        }

        [MonoPInvokeCallback(typeof(ErrorCallbackDelegate))]
        private static void ErrorCallback(IntPtr state, IntPtr messagePointer)
        {
            var handle = GCHandle.FromIntPtr(state);
            var source = (IDBValueTaskSource)handle.Target;
            var message = Marshal.PtrToStringUTF8(messagePointer) ?? "IndexedDB operation failed.";
            source.SetException(new Exception(message));
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void NonLoadSuccessCallbackDelegate(IntPtr state);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void LoadSuccessCallbackDelegate(IntPtr state, IntPtr dataPointer, int length);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void ErrorCallbackDelegate(IntPtr state, IntPtr messagePointer);
    }
}

#endif