#if UNITY_WEBGL
#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AndanteTribe.IO.Unity
{
    /// <summary>
    /// Represents a stream for IndexedDB operations.
    /// </summary>
    /// <remarks>No multi-threading support because multi-threading is not allowed in the WebGL environment.</remarks>
    public class IDBStream : Stream
    {
        private readonly string _path;
        private byte[] _buffer = Array.Empty<byte>();
        private int _written;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException("Length is not supported for IndexedDBStream.");

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException("Position is not supported for IndexedDBStream.");
            set => throw new NotSupportedException("Position is not supported for IndexedDBStream.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IDBStream"/> class with the specified path.
        /// </summary>
        /// <param name="path">The key to the IndexedDB.</param>
        public IDBStream(string path) => _path = path;

        /// <inheritdoc />
        public override void Flush()
        {
            // Flush is typically implemented as an empty method to ensure full compatibility with other Stream types.
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous Read is not supported in WebGL. Use ReadAsync instead.");

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
            ReadAsync(new Memory<byte>(buffer, offset, count), cancellationToken).AsTask();

        /// <inheritdoc />
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var source = IDBValueTaskSource.Create();
            source.Buffer = buffer;
            await using var _ = cancellationToken.RegisterWithoutCaptureExecutionContext(static s =>
            {
                ((IDBValueTaskSource)s).SetCanceled();
            }, source);

            IDBUtils.LoadFromIndexedDB(source.Handle, _path, IDBUtils.LoadSuccessCallback, IDBUtils.ErrorCallback);
            return (await new ValueTask<(byte[] _, int size)>(source, source.Version)).size;
        }

        /// <inheritdoc />
        public override int ReadByte() =>
            throw new NotSupportedException("Synchronous ReadByte is not supported in WebGL. Use ReadAsync instead.");

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Seek is not supported for IndexedDBStream.");

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException("SetLength is not supported for IndexedDBStream.");

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) =>
            throw new NotSupportedException("Synchronous Write is not supported in WebGL. Use WriteAsync instead.");

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteBuffer(new ReadOnlySpan<byte>(buffer, offset, count));
            return IDBUtils.WriteAllBytesAsync(_path, new ReadOnlyMemory<byte>(_buffer, 0, _written), cancellationToken).AsTask();
        }

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteBuffer(buffer.Span);
            return IDBUtils.WriteAllBytesAsync(_path, new ReadOnlyMemory<byte>(_buffer, 0, _written), cancellationToken);
        }

        private void WriteBuffer(in ReadOnlySpan<byte> value)
        {
            if (_buffer.Length < _written + value.Length)
            {
                Array.Resize(ref _buffer, _written + value.Length);
            }

            value.CopyTo(_buffer.AsSpan()[_written..]);
            _written += value.Length;
        }
    }
}

#endif