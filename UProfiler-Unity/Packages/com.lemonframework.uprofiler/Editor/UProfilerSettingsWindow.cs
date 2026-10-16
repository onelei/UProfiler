#if UPROFILER
using LemonFramework.UProfiler.Core;
using LemonFramework.UProfiler.Components;
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
        const string MenuPath = "UProfiler/Profiler Settings";
        const string AdvancedFoldoutKey = "UProfiler.ProfilerSettings.ShowAdvanced";

        UProfilerSettings _settings;
        SerializedObject _serializedSettings;
        bool _showAdvanced;

        [MenuItem(MenuPath)]
        public static void Open()
        {
            var window = GetWindow<UProfilerSettingsWindow>(false, "Profiler Settings", true);
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        void OnEnable()
        {
            _settings = UProfilerSettingsAssetUtility.GetOrCreate();
            _serializedSettings = new SerializedObject(_settings);
            _showAdvanced = EditorPrefs.GetBool(AdvancedFoldoutKey, false);
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

            EditorGUILayout.LabelField("Profiler Settings", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Runtime capture switches (including Function Analysis) are on the UProfilerHost component in your scene.\n" +
                "This panel only holds Editor-only options and advanced diagnostics.",
                MessageType.None);

            DrawEditorInjectionSection();
            EditorGUILayout.Space(8f);
            DrawUtilitiesSection();
            EditorGUILayout.Space(8f);
            DrawAdvancedSection();
        }

        void DrawEditorInjectionSection()
        {
            EditorGUILayout.LabelField("Editor Injection", EditorStyles.boldLabel);
            _serializedSettings.Update();
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(
                _serializedSettings.FindProperty(nameof(UProfilerSettings.autoInjectFunctionAnalysisOnCompile)),
                new GUIContent(
                    "Auto Inject On Compile",
                    "While UPROFILER is defined, inject [FunctionAnalysis] during Editor and Development Build compilation."));

            if (EditorGUI.EndChangeCheck())
            {
                _serializedSettings.ApplyModifiedProperties();
                EditorUtility.SetDirty(_settings);
                AssetDatabase.SaveAssets();
                UProfilerSettings.InvalidateCache();
                UProfilerHookCompilationInjector.SetAutoInject(_settings.autoInjectFunctionAnalysisOnCompile);
            }
            else
            {
                _serializedSettings.ApplyModifiedProperties();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(
                "Turn the whole package off by removing the UPROFILER scripting define.\n" +
                "Function Analysis on/off: use UProfilerHost → enableFunctionAnalysis.\n" +
                "Injection runs while compiling the Editor or a Development Build. Release players are not patched.\n" +
                "Manual inject / export: expand Advanced below",
                MessageType.Info);
        }

        void DrawUtilitiesSection()
        {
            EditorGUILayout.LabelField("Utilities", EditorStyles.boldLabel);
            if (GUILayout.Button(new GUIContent("Open Local Record Folder", Application.persistentDataPath)))
            {
                EditorUtility.OpenWithDefaultApp(Application.persistentDataPath);
            }
        }

        void DrawAdvancedSection()
        {
            EditorGUI.BeginChangeCheck();
            _showAdvanced = EditorGUILayout.BeginFoldoutHeaderGroup(
                _showAdvanced,
                new GUIContent("Advanced", "Manual IL inject, debug logging, and local function timing export."));
            if (EditorGUI.EndChangeCheck())
                EditorPrefs.SetBool(AdvancedFoldoutKey, _showAdvanced);

            if (_showAdvanced)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                var analysisEnabled = UProfilerHost.IsFunctionAnalysisEnabled;
                var canInject = analysisEnabled && !Application.isPlaying && !EditorApplication.isCompiling;

                EditorGUILayout.LabelField("IL Inject", EditorStyles.miniBoldLabel);
                if (!analysisEnabled)
                {
                    EditorGUILayout.HelpBox("Function analysis is off on UProfilerHost. Enable enableFunctionAnalysis to inject.", MessageType.Warning);
                }
                else if (!canInject)
                {
                    EditorGUILayout.HelpBox("Stop Play mode and wait for compilation to finish before injecting.", MessageType.Warning);
                }

                using (new EditorGUI.DisabledScope(!canInject))
                {
                    DrawActionButton(
                        "Inject [FunctionAnalysis] Methods",
                        "Inject HookUtil.Begin/End into methods marked with [FunctionAnalysis]. Recommended manual fallback.",
                        UProfilerHookEditor.HookFunctionAnalysisInject);
                    DrawActionButton(
                        "Inject [ProfilerSample] Methods",
                        "Inject Unity Profiler.BeginSample into methods marked with [ProfilerSample].",
                        UProfilerHookEditor.HookProfilerSampleInject);
                    DrawActionButton(
                        "Inject All Methods (Profiler)",
                        "Inject timing hooks into almost every method. High overhead — debug only.",
                        UProfilerHookEditor.HookInject);
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Debug Log Inject", EditorStyles.miniBoldLabel);
                using (new EditorGUI.DisabledScope(!canInject))
                {
                    DrawActionButton(
                        "Log All Method Entry Exit",
                        "Inject Debug.Log at method entry/exit. Very verbose.",
                        UProfilerHookEditor.HookLogAllFunction);
                    DrawActionButton(
                        "Log All Except Update / OnGUI",
                        "Same as above but skips Update and OnGUI.",
                        UProfilerHookEditor.HookLogAllFunctionExceptUpdate);
                }

                EditorGUILayout.Space(6f);
                EditorGUILayout.LabelField("Runtime Tools", EditorStyles.miniBoldLabel);
                using (new EditorGUI.DisabledScope(!analysisEnabled))
                {
                    DrawActionButton(
                        "Print Method Timings",
                        "Print [FunctionAnalysis] timing summary to the Console.",
                        UProfilerHookEditor.ShowFuncAnaysics);
                    DrawActionButton(
                        "Export Function Performance Report",
                        "Export Hook timing data to persistentDataPath as CSV/TXT.",
                        UProfilerHookEditor.HookUtilsReport);
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        static void DrawActionButton(string label, string tooltip, System.Action action)
        {
            if (GUILayout.Button(new GUIContent(label, tooltip), GUILayout.Height(22f)))
                action();
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
            settings.autoInjectFunctionAnalysisOnCompile = true;
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            UProfilerSettings.InvalidateCache();
            return settings;
        }
    }
}
#endif
