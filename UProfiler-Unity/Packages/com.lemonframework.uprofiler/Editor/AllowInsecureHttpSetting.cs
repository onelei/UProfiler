#if UNITY_EDITOR
using UnityEditor;

namespace LemonFramework.UProfiler.Editor
{
    [InitializeOnLoad]
    public static class AllowInsecureHttpSetting
    {
        static AllowInsecureHttpSetting()
        {
            if (PlayerSettings.insecureHttpOption != InsecureHttpOption.AlwaysAllowed)
            {
                PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;
                AssetDatabase.SaveAssets();
                UnityEngine.Debug.Log("[UProfiler] PlayerSettings.insecureHttpOption set to AlwaysAllowed.");
            }
        }
    }
}
#endif