#if UNITY_WEBGL
#nullable enable

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace AndanteTribe.IO.Unity
{
    internal sealed class IDBValueTaskSource : IValueTaskSource<(byte[], int)>, IValueTaskSource
    {
        private static IDBValueTaskSource? s_head;
        private IDBValueTaskSource? _next;

        private ManualResetValueTaskSourceCore<(byte[], int)> _core = new()
        {
            RunContinuationsAsynchronously = false
        };
        private GCHandle _handle;
        private bool _isCompleted;
        private bool _isNativeCompleted;
        private bool _isConsumerCompleted;

        public IntPtr Handle => GCHandle.ToIntPtr(_handle);
        public Memory<byte> Buffer { get; set; }

        private IDBValueTaskSource()
        {
        }

        public static IDBValueTaskSource Create()
        {
            var instance = s_head;
            if (instance != null)
            {
                s_head = instance._next;
                instance._next = null;
            }
            else
            {
                instance = new IDBValueTaskSource();
            }

            instance._handle = GCHandle.Alloc(instance);
            return instance;
        }

        public void SetResult()
        {
            if (!TryBeginNativeCompletion())
            {
                return;
            }

            try
            {
                if (!_isCompleted)
                {
                    _isCompleted = true;
                    _core.SetResult((Array.Empty<byte>(), 0));
                }
            }
            finally
            {
                FinishNativeCompletion();
            }
        }

        public unsafe void SetResult(IntPtr dataPtr, int length)
        {
            if (!TryBeginNativeCompletion())
            {
                return;
            }

            try
            {
                if (!_isCompleted)
                {
                    var dataSpan = new Span<byte>(dataPtr.ToPointer(), length);
                    if (!Buffer.IsEmpty)
                    {
                        var size = Math.Min(length, Buffer.Length);
                        dataSpan[..size].CopyTo(Buffer.Span);
                        _isCompleted = true;
                        _core.SetResult((Array.Empty<byte>(), size));
                    }
                    else
                    {
                        var data = dataSpan.ToArray();
                        _isCompleted = true;
                        _core.SetResult((data, length));
                    }
                }
            }
            finally
            {
                FinishNativeCompletion();
            }
        }

        public void SetException(Exception error)
        {
            if (!TryBeginNativeCompletion())
            {
                return;
            }

            try
            {
                if (!_isCompleted)
                {
                    _isCompleted = true;
                    _core.SetException(error);
                }
            }
            finally
            {
                FinishNativeCompletion();
            }
        }

        public void SetCanceled()
        {
            if (_isCompleted)
            {
                return;
            }

            _isCompleted = true;
            _core.SetException(new TaskCanceledException());
        }

        public short Version => _core.Version;

        [DebuggerNonUserCode]
        public (byte[], int) GetResult(short token)
        {
            try
            {
                return _core.GetResult(token);
            }
            finally
            {
                CompleteConsumer();
            }
        }

        void IValueTaskSource.GetResult(short token)
        {
            try
            {
                _core.GetResult(token);
            }
            finally
            {
                CompleteConsumer();
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);

        private bool TryBeginNativeCompletion()
        {
            if (_isNativeCompleted)
            {
                return false;
            }

            _isNativeCompleted = true;
            return true;
        }

        private void FinishNativeCompletion()
        {
            _handle.Free();
            TryReset();
        }

        private void CompleteConsumer()
        {
            _isConsumerCompleted = true;
            TryReset();
        }

        private void TryReset()
        {
            if (!_isNativeCompleted || !_isConsumerCompleted)
            {
                return;
            }

            _core.Reset();
            Buffer = default;
            _isCompleted = false;
            _isNativeCompleted = false;
            _isConsumerCompleted = false;
            _next = s_head;
            s_head = this;
        }
    }
}

#endif