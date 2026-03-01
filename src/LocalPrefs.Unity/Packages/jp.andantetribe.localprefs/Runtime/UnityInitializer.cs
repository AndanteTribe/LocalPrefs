#nullable enable
using System;
using UnityEngine;

[assembly: UnityEngine.Scripting.AlwaysLinkAssembly]

namespace AndanteTribe.IO.Unity
{
#if UNITY_EDITOR
    [UnityEditor.InitializeOnLoad]
#endif
    internal static class UnityInitializer
    {
        static UnityInitializer() => SetDefaultShared();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void SetDefaultShared()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            var fileAccessor = new LSAccessor("localprefs-shared");
#else
            var fileAccessor = FileAccessor.Create(Application.persistentDataPath + "/localprefs-shared");
#endif

#if ENABLE_MESSAGEPACK
            LocalPrefs.Shared = new MessagePack.MessagePackLocalPrefs(fileAccessor);
#else
            var localPrefsType = Type.GetType("AndanteTribe.IO.Json.JsonLocalPrefs, LocalPrefs.Json");
            localPrefsType ??= Type.GetType("AndanteTribe.IO.MessagePack.MessagePackLocalPrefs, LocalPrefs.MessagePack");
            if (localPrefsType == null)
            {
                Debug.LogWarning("Failed to initialize LocalPrefs.Shared: No supported serializer found.");
                return;
            }
            LocalPrefs.Shared = (ILocalPrefs)Activator.CreateInstance(localPrefsType, fileAccessor)!;
#endif
        }
    }
}