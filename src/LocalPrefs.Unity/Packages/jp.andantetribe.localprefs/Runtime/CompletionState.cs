#if UNITY_WEBGL
#nullable enable

using System;

namespace AndanteTribe.IO.Unity
{
    [Flags]
    internal enum CompletionState : byte
    {
        None = 0,
        ManagedCompleted = 1 << 0,
        NativeCompletionStarted = 1 << 1,
        // Pooling must wait for native cleanup, which runs after a potentially synchronous continuation.
        NativeCompleted = 1 << 2,
        ConsumerCompleted = 1 << 3
    }

    internal static class CompletionStateExtensions
    {
        internal static bool HasAnyFlag(this CompletionState state, CompletionState flags)
            => (state & flags) != 0;

        internal static bool HasAllFlags(this CompletionState state, CompletionState flags)
            => (state & flags) == flags;

        internal static void AddFlag(this ref CompletionState state, CompletionState flag)
            => state |= flag;

        internal static bool TryAddFlag(this ref CompletionState state, CompletionState flag)
        {
            if (state.HasAnyFlag(flag))
            {
                return false;
            }

            state.AddFlag(flag);
            return true;
        }
    }
}

#endif
