#if ENABLE_DEBUGTOOLKIT

using AndanteTribe.IO;
using AndanteTribe.IO.Unity;
using DebugToolkit;
using UnityEngine;
using UnityEngine.UIElements;

public class DebugToolEntryPoint : MonoBehaviour
{
    private void Start() => new DebugView().Start();
}

public sealed class DebugView : DebugViewerBase
{
    protected override VisualElement CreateViewGUI()
    {
        var root = base.CreateViewGUI();
        var prefsDebugWindow = root.AddWindow(nameof(LocalPrefs));
        prefsDebugWindow.style.width = Screen.width * 0.8f;
        prefsDebugWindow.style.height = Screen.height * 0.8f;
        prefsDebugWindow.AddLocalPrefsDebugMenu();
        return root;
    }
}

#endif