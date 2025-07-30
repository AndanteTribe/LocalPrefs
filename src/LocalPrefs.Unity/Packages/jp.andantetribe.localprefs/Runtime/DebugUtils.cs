#if ENABLE_UITOOLKIT
#nullable enable

using System.Runtime.CompilerServices;
using UnityEngine.UIElements;

namespace AndanteTribe.IO.Unity
{
    /// <summary>
    /// Provides utility methods for debugging purposes in the LocalPrefs system.
    /// </summary>
    public static class DebugUtils
    {
        /// <summary>
        /// Adds the LocalPrefs debug menu to the given parent element.
        /// </summary>
        /// <param name="parent"> The parent VisualElement to which the debug menu will be added.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddLocalPrefsDebugMenu(this VisualElement parent)
        {
            parent.Add(new Button() { text = nameof(LocalPrefs) });
        }
    }
}

#endif