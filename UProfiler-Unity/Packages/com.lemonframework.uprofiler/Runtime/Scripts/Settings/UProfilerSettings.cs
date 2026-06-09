using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    public class UProfilerSettings : ScriptableObject
    {
        const string ResourceName = "UProfilerSettings";

        [Tooltip("Enable IL-injected function hook profiling. Turn off when running performance tests that should exclude hook overhead.")]
        public bool enableFunctionHook;

        [Tooltip("After script compile, auto inject [FunctionAnalysis] methods into Assembly-CSharp.dll.")]
        public bool autoInjectFunctionAnalysisOnCompile = true;

        static UProfilerSettings _cached;

        public static bool IsFunctionHookEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                var settings = Instance;
                return settings != null && settings.enableFunctionHook;
#else
                return false;
#endif
            }
        }

        public static UProfilerSettings Instance
        {
            get
            {
                if (_cached == null)
                    _cached = Resources.Load<UProfilerSettings>(ResourceName);
                return _cached;
            }
        }

        public static void InvalidateCache()
        {
            _cached = null;
        }
    }
}
