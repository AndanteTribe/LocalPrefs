#if UNITY_WEBGL
#nullable enable

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace AndanteTribe.IO.Unity
{
    internal sealed class IDBValueTaskSource : IValueTaskSource<(byte[], int)>, IValueTaskSource
    {
        private static IDBValueTaskSource? s_head;

        private ManualResetValueTaskSourceCore<(byte[], int)> _core = new()
        {
            RunContinuationsAsynchronously = false
        };

        public Memory<byte> Buffer { get; set; }

        private IDBValueTaskSource? _next;

        private IDBValueTaskSource()
        {
        }

        public static IDBValueTaskSource Create()
        {
            if (s_head != null)
            {
                var instance = s_head;
                s_head = instance._next;
                instance._next = null;
                return instance;
            }
            return new IDBValueTaskSource();
        }

        public void SetResult() => _core.SetResult((Array.Empty<byte>(), 0));

        public unsafe void SetResult(IntPtr dataPtr, int length)
        {
            var dataSpan = new Span<byte>(dataPtr.ToPointer(), length);
            if (!Buffer.IsEmpty)
            {
                var size = Math.Min(length, Buffer.Length);
                dataSpan[..size].CopyTo(Buffer.Span);
                _core.SetResult((Array.Empty<byte>(), size));
            }
            else
            {
                _core.SetResult((dataSpan.ToArray(), length));
            }
        }

        public void SetException(Exception error) => _core.SetException(error);

        public void SetCanceled() => _core.SetException(new TaskCanceledException());

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
                Reset();
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
                Reset();
            }
        }

        public ValueTaskSourceStatus GetStatus(short token) => _core.GetStatus(token);

        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            => _core.OnCompleted(continuation, state, token, flags);

        private void Reset()
        {
            _core.Reset();
            Buffer = default;
            _next = s_head;
            s_head = this;
        }
    }
}

#endif