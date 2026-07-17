#if UNITY_WEBGL
#nullable enable

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace AndanteTribe.IO.Unity
{
    internal static unsafe class LocalPrefsNativeInterop
    {
        public static void SaveToLocalStorage(string key, string value)
        {
            var keyBytes = GetNullTerminatedUtf8Bytes(key);
            var valueBytes = GetNullTerminatedUtf8Bytes(value);
            fixed (byte* keyPointer = keyBytes)
            fixed (byte* valuePointer = valueBytes)
            {
                LocalPrefsNative.local_prefs_save_to_local_storage(keyPointer, valuePointer);
            }
        }

        public static void DeleteFromLocalStorage(string key)
        {
            var keyBytes = GetNullTerminatedUtf8Bytes(key);
            fixed (byte* keyPointer = keyBytes)
            {
                LocalPrefsNative.local_prefs_delete_from_local_storage(keyPointer);
            }
        }

        public static string? LoadFromLocalStorage(string key)
        {
            var keyBytes = GetNullTerminatedUtf8Bytes(key);
            fixed (byte* keyPointer = keyBytes)
            {
                var valuePointer = LocalPrefsNative.local_prefs_load_from_local_storage(keyPointer);
                if (valuePointer == null)
                {
                    return null;
                }

                try
                {
                    return Marshal.PtrToStringUTF8((IntPtr)valuePointer);
                }
                finally
                {
                    LocalPrefsNative.local_prefs_free(valuePointer);
                }
            }
        }

        public static void SaveToIndexedDB(IntPtr state, string key, byte[] data, int dataSize, IntPtr success, IntPtr error)
        {
            fixed (byte* dataPointer = data)
            {
                SaveToIndexedDB(state, key, (IntPtr)dataPointer, dataSize, success, error);
            }
        }

        public static void SaveToIndexedDB(IntPtr state, string key, IntPtr data, int dataSize, IntPtr success, IntPtr error)
        {
            var keyBytes = GetNullTerminatedUtf8Bytes(key);
            fixed (byte* keyPointer = keyBytes)
            {
                LocalPrefsNative.local_prefs_save_to_indexed_db(
                    state.ToPointer(),
                    keyPointer,
                    (byte*)data.ToPointer(),
                    dataSize,
                    success.ToPointer(),
                    error.ToPointer());
            }
        }

        public static void DeleteFromIndexedDB(IntPtr state, string key, IntPtr success, IntPtr error)
        {
            var keyBytes = GetNullTerminatedUtf8Bytes(key);
            fixed (byte* keyPointer = keyBytes)
            {
                LocalPrefsNative.local_prefs_delete_from_indexed_db(
                    state.ToPointer(),
                    keyPointer,
                    success.ToPointer(),
                    error.ToPointer());
            }
        }

        public static void LoadFromIndexedDB(IntPtr state, string key, IntPtr success, IntPtr error)
        {
            var keyBytes = GetNullTerminatedUtf8Bytes(key);
            fixed (byte* keyPointer = keyBytes)
            {
                LocalPrefsNative.local_prefs_load_from_indexed_db(
                    state.ToPointer(),
                    keyPointer,
                    success.ToPointer(),
                    error.ToPointer());
            }
        }

        private static byte[] GetNullTerminatedUtf8Bytes(string value) => Encoding.UTF8.GetBytes(value + '\0');
    }
}

#endif