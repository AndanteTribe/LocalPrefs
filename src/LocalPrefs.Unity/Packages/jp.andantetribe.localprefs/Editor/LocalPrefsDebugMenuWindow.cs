#nullable enable

using UnityEditor;
using UnityEngine;

namespace AndanteTribe.IO.Unity.Editor
{
    public class LocalPrefsDebugMenuWindow : EditorWindow
    {
        [MenuItem("Window/LocalPrefs/Debug Menu")]
        public static void ShowWindow()
        {
            var window = GetWindow<LocalPrefsDebugMenuWindow>();
            window.titleContent = new GUIContent("LocalPrefs Debug Menu");
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.AddLocalPrefsDebugMenu();
        }
    }
}