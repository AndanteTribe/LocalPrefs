namespace AndanteTribe.IO;

/// <summary>
/// Abstract class that provides access to the file system.
/// Abstracts file read/write operations for local preferences.
/// </summary>
public abstract class FileAccessor
{
    /// <summary>
    /// The file path where preference data will be stored.
    /// </summary>
    protected internal abstract string SavePath { get; }

    /// <summary>
    /// Reads the entire file into a byte array.
    /// </summary>
    /// <returns>A byte array containing the file's contents.</returns>
    public abstract byte[] ReadAllBytes();

    /// <summary>
    /// Writes a byte array to the file asynchronously.
    /// </summary>
    /// <param name="bytes">Bytes to write to the file.</param>
    /// <param name="cancellationToken"> A token to cancel the asynchronous operation.</param>
    public abstract ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file asynchronously.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    public abstract ValueTask DeleteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a default <see cref="FileAccessor"/> for the specified path.
    /// </summary>
    /// <param name="path">Path to the file where preference data will be stored.</param>
    /// <returns>An instance of <see cref="FileAccessor"/> that uses the default file system operations.</returns>
    public static FileAccessor Create(in string path) => new DefaultFileAccessor(path);

    /// <summary>
    /// Default implementation of the FileAccessor interface.
    /// Provides standard file system operations using <see cref="System.IO"/>.
    /// </summary>
    /// <param name="path">The file path where preference data will be stored. The file will be created if it doesn't exist.</param>
    private sealed class DefaultFileAccessor(in string path) : FileAccessor
    {
        /// <inheritdoc />
        protected internal override string SavePath { get; } = path;

        /// <inheritdoc />
        public override byte[] ReadAllBytes() =>
            File.Exists(SavePath) ? File.ReadAllBytes(SavePath) : [];

        /// <inheritdoc />
        public override async ValueTask WriteAsync(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = File.Create(SavePath, 4096, FileOptions.Asynchronous);
            await stream.WriteAsync(bytes, cancellationToken);
        }

        /// <inheritdoc />
        public override ValueTask DeleteAsync(CancellationToken cancellationToken = default)
        {
            if (File.Exists(SavePath))
            {
                File.Delete(SavePath);
            }
            return default;
        }
    }
}