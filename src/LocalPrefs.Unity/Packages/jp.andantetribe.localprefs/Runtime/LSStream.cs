﻿#if UNITY_WEBGL
#nullable enable

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Unity.Collections;

namespace AndanteTribe.IO.Unity
{
    /// <summary>
    /// Represents a stream that reads from and writes to Local Storage in WebGL builds.
    /// </summary>
    public class LSStream : Stream
    {
        private readonly string _path;
        private NativeArray<byte> _buffer;
        private int _written;

        /// <inheritdoc />
        public override bool CanRead => true;

        /// <inheritdoc />
        public override bool CanSeek => false;

        /// <inheritdoc />
        public override bool CanWrite => true;

        /// <inheritdoc />
        public override long Length => throw new NotSupportedException("Length is not supported for LSStream.");

        /// <inheritdoc />
        public override long Position
        {
            get => throw new NotSupportedException("Position is not supported for LSStream.");
            set => throw new NotSupportedException("Position is not supported for LSStream.");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LSStream"/> class with the specified path.
        /// </summary>
        /// <param name="path"> The key to the Local Storage file.</param>
        public LSStream(string path) => _path = path;

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (_buffer.IsCreated)
            {
                _buffer.Dispose();
            }
            base.Dispose(disposing);
        }

        /// <inheritdoc />
        public override void Flush()
        {
            // Flush is typically implemented as an empty method to ensure full compatibility with other Stream types.
        }

        /// <inheritdoc />
        public override int Read(byte[] buffer, int offset, int count)
        {
            var data = LSUtils.ReadAllText(_path);
            return Convert.TryFromBase64String(data, buffer.AsSpan(offset, count), out var written) ? written : 0;
        }

        /// <inheritdoc />
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = Read(buffer, offset, count);
            return Task.FromResult(result);
        }

        /// <inheritdoc />
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var data = LSUtils.ReadAllText(_path);
            return new ValueTask<int>(Convert.TryFromBase64String(data, buffer.Span, out var written) ? written : 0);
        }

        /// <inheritdoc />
        public override int ReadByte()
        {
            var data = LSUtils.ReadAllText(_path);
            var buffer = (Span<byte>)stackalloc byte[1];
            if (!Convert.TryFromBase64String(data, buffer, out var written) || written == 0)
            {
                return -1;
            }

            return buffer[0];
        }

        /// <inheritdoc />
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException("Seek is not supported for LSStream.");

        /// <inheritdoc />
        public override void SetLength(long value) => throw new NotSupportedException("SetLength is not supported for LSStream.");

        /// <inheritdoc />
        public override void Write(byte[] buffer, int offset, int count) =>
            LSUtils.WriteAllBytes(_path, WriteBuffer(new ReadOnlySpan<byte>(buffer, offset, count)));

        /// <inheritdoc />
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Write(buffer, offset, count);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LSUtils.WriteAllBytes(_path, WriteBuffer(buffer.Span));
            return default;
        }

        private ReadOnlySpan<byte> WriteBuffer(in ReadOnlySpan<byte> value)
        {
            if (!_buffer.IsCreated && _buffer.Length != 0)
            {
                throw new ObjectDisposedException(nameof(LSStream));
            }
            if (_buffer.Length < _written + value.Length)
            {
                var newBuffer = new NativeArray<byte>(_written + value.Length, Allocator.Persistent);
                if (_buffer.Length != 0)
                {
                    _buffer.CopyTo(newBuffer);
                    _buffer.Dispose();
                }
                _buffer = newBuffer;
            }

            value.CopyTo(_buffer.AsSpan()[_written..]);
            _written += value.Length;
            return _buffer.AsSpan()[.._written];
        }
    }
}

#endif
