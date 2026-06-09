using LemonFramework.UProfiler.Core;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace LemonFramework.UProfiler.Editor
{
    [InitializeOnLoad]
    static class UProfilerSettingsBootstrap
    {
        static UProfilerSettingsBootstrap()
        {
            UProfilerSettingsAssetUtility.GetOrCreate();
        }
    }

    public class UProfilerSettingsWindow : EditorWindow
    {
        const string MenuPath = "UProfiler/Settings";

        UProfilerSettings _settings;
        SerializedObject _serializedSettings;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<UProfilerSettingsWindow>(true, "UProfiler Settings", true);
            window.minSize = new Vector2(380f, 220f);
            window.Show();
        }

        void OnEnable()
        {
            _settings = UProfilerSettingsAssetUtility.GetOrCreate();
            _serializedSettings = new SerializedObject(_settings);
        }

        void OnGUI()
        {
            if (_settings == null || _serializedSettings == null)
            {
                EditorGUILayout.HelpBox("Failed to load UProfiler settings.", MessageType.Error);
                if (GUILayout.Button("Retry"))
                    OnEnable();
                return;
            }

            EditorGUILayout.LabelField("Function Hook", EditorStyles.boldLabel);
            _serializedSettings.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty(nameof(UProfilerSettings.enableFunctionHook)),
                new GUIContent(
                    "Enable Function Hook",
                    "When enabled, Hook menu items and runtime function sampling are available."));
            using (new EditorGUI.DisabledScope(!_settings.enableFunctionHook))
            {
                EditorGUILayout.PropertyField(
                    _serializedSettings.FindProperty(nameof(UProfilerSettings.autoInjectFunctionAnalysisOnCompile)),
                    new GUIContent(
                        "Auto Inject On Compile",
                        "Inject [FunctionAnalysis] into Assembly-CSharp.dll right after each script compile."));
            }

            if (EditorGUI.EndChangeCheck())
            {
                _serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                UProfilerSettings.InvalidateCache();
            }
            else
            {
                _serializedSettings.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(8f);
            if (_settings.enableFunctionHook)
            {
                EditorGUILayout.HelpBox(
                    "Function Hook is ON.\n" +
                    "• [FunctionAnalysis] is auto-injected before Play and after compile (if enabled)\n" +
                    "• Enter Play, run profiled methods, then Print Method Timings\n" +
                    "• ProfilerSample inject is separate and does not fill Print Method Timings",
                    MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Function Hook is OFF. Hook menus are disabled and injected methods will not collect timing data.\n" +
                    "Use this mode when performance testing should exclude function hook overhead.",
                    MessageType.Warning);
            }
        }
    }

    static class UProfilerSettingsAssetUtility
    {
        public const string AssetPath = "Packages/com.lemonframework.uprofiler/Runtime/Resources/UProfilerSettings.asset";

        public static UProfilerSettings GetOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<UProfilerSettings>(AssetPath);
            if (settings != null)
                return settings;

            var directory = Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            settings = ScriptableObject.CreateInstance<UProfilerSettings>();
            settings.enableFunctionHook = false;
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            UProfilerSettings.InvalidateCache();
            return settings;
        }
    }
}
