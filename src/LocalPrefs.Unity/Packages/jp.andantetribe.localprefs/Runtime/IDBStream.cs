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
    /// <remarks>
    /// No multi-threading support because multi-threading is not allowed in the WebGL environment.
    /// Writes are buffered until <see cref="FlushAsync(CancellationToken)"/> or <see cref="DisposeAsync"/> is called.
    /// </remarks>
    public class IDBStream : Stream
    {
        private readonly string _path;
        private byte[] _buffer = Array.Empty<byte>();
        private int _written;
        private int _writeVersion;
        private bool _isDirty;
        private bool _isDisposed;

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
            ThrowIfDisposed();
            if (_isDirty)
            {
                throw new NotSupportedException("Synchronous Flush is not supported in WebGL. Use FlushAsync instead.");
            }
        }

        /// <inheritdoc />
        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();
            cancellationToken.ThrowIfCancellationRequested();
            if (!_isDirty)
            {
                return;
            }

            var flushedVersion = _writeVersion;
            await IDBUtils.WriteAllBytesAsync(_path, new ReadOnlyMemory<byte>(_buffer, 0, _written), cancellationToken);
            if (_writeVersion == flushedVersion)
            {
                _isDirty = false;
            }
        }

        /// <inheritdoc />
        public override async ValueTask DisposeAsync()
        {
            if (_isDisposed)
            {
                return;
            }

            await FlushAsync(CancellationToken.None);
            Dispose();
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_isDisposed)
            {
                if (_isDirty)
                {
                    throw new InvalidOperationException("The stream has buffered data. Use DisposeAsync to persist it to IndexedDB.");
                }

                _buffer = Array.Empty<byte>();
                _isDisposed = true;
            }

            base.Dispose(disposing);
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
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WriteBuffer(buffer.Span);
            return default;
        }

        private void WriteBuffer(in ReadOnlySpan<byte> value)
        {
            ThrowIfDisposed();
            if (value.IsEmpty)
            {
                return;
            }

            var requiredLength = checked(_written + value.Length);
            if (_buffer.Length < requiredLength)
            {
                var doubledLength = _buffer.Length > int.MaxValue / 2 ? int.MaxValue : _buffer.Length * 2;
                var newLength = _buffer.Length == 0 ? requiredLength : Math.Max(requiredLength, doubledLength);
                Array.Resize(ref _buffer, newLength);
            }

            value.CopyTo(_buffer.AsSpan()[_written..]);
            _written += value.Length;
            _writeVersion++;
            _isDirty = true;
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(IDBStream));
            }
        }
    }
}

#endif