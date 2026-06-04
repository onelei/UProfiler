using UnityEditor;
using UnityEngine;

namespace LemonFramework.UProfiler.Editor
{
    public static class MenuItems
    {
        [MenuItem("UProfiler/Download")]
        private static void ViewDownload()
        {
            EditorUtility.OpenWithDefaultApp(Application.persistentDataPath);
        }
    }
}
