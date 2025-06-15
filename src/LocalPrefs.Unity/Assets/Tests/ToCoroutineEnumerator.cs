#nullable enable

using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

namespace AndanteTribe.IO.Unity.Tests
{
    internal sealed class ToCoroutineEnumerator : IEnumerator
    {
        private readonly Func<ValueTask> _task;
        private readonly Action<Exception>? _exceptionHandler = null;

        private bool _completed = false;
        private bool _isStarted = false;
        private ExceptionDispatchInfo? _exception;

        public ToCoroutineEnumerator(Func<ValueTask> task, Action<Exception>? exceptionHandler = null)
        {
            _exceptionHandler = exceptionHandler;
            _task = task;
        }

        private async ValueTask RunTask()
        {
            try
            {
                await _task();
            }
            catch (Exception ex)
            {
                if (_exceptionHandler != null)
                {
                    _exceptionHandler(ex);
                }
                else
                {
                    _exception = ExceptionDispatchInfo.Capture(ex);
                }
            }
            finally
            {
                _completed = true;
            }
        }

        public object Current => null!;

        public bool MoveNext()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                _ = RunTask();
            }

            if (_exception != null)
            {
                _exception.Throw();
                return false;
            }

            return !_completed;
        }

        void IEnumerator.Reset() => throw new NotSupportedException("Reset is not supported for this enumerator.");
    }
}