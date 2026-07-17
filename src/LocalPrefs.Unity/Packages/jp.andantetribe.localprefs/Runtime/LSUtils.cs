#if UNITY_WEBGL
#nullable enable

using System;

namespace AndanteTribe.IO.Unity
{
    /// <summary>
    /// Provides utility methods for interacting with Local Storage in a WebGL environment.
    /// </summary>
    public static class LSUtils
    {
        /// <summary>
        /// Writes the specified byte array to Local Storage using the specified path as key.
        /// </summary>
        /// <param name="path"> The path string that serves as the key.</param>
        /// <param name="bytes"> The bytes to write to Local Storage.</param>
        public static void WriteAllBytes(in string path, in ReadOnlySpan<byte> bytes) =>
            LocalPrefsNativeInterop.SaveToLocalStorage(path, Convert.ToBase64String(bytes));

        /// <summary>
        /// Writes the specified string to Local Storage using the specified path as key.
        /// </summary>
        /// <param name="path"> The path string that serves as the key.</param>
        /// <param name="contents"> The string to write to Local Storage.</param>
        public static void WriteAllText(in string path, in string contents) =>
            LocalPrefsNativeInterop.SaveToLocalStorage(path, contents);

        /// <summary>
        /// Deletes the specified path from Local Storage.
        /// </summary>
        /// <param name="path"> The path string that serves as the key.</param>
        public static void Delete(in string path) => LocalPrefsNativeInterop.DeleteFromLocalStorage(path);

        /// <summary>
        /// Reads all bytes from Local Storage using the specified path as key.
        /// </summary>
        /// <param name="path"> The path string that serves as the key.</param>
        /// <returns> A byte array containing the data read from Local Storage.</returns>
        public static byte[] ReadAllBytes(in string path)
        {
            var data = LocalPrefsNativeInterop.LoadFromLocalStorage(path);
            if (string.IsNullOrEmpty(data))
            {
                return Array.Empty<byte>();
            }

            var bytes = Convert.FromBase64String(data);
            return bytes;
        }

        /// <summary>
        /// Reads all text from Local Storage using the specified path as key.
        /// </summary>
        /// <param name="path"> The path string that serves as the key.</param>
        /// <returns> A string containing the data read from Local Storage.</returns>
        public static string ReadAllText(in string path)
        {
            var data = LocalPrefsNativeInterop.LoadFromLocalStorage(path);
            return string.IsNullOrEmpty(data) ? "" : data;
        }
    }
}

#endif