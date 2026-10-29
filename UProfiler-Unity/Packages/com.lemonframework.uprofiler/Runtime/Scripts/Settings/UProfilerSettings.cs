#if UPROFILER
using LemonFramework.UProfiler.Components;
using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    public class UProfilerSettings : ScriptableObject
    {
        const string ResourceName = "UProfilerSettings";

        [Tooltip("After script compile, auto inject [FunctionAnalysis] methods into game assemblies.")]
        public bool autoInjectFunctionAnalysisOnCompile = true;

        static UProfilerSettings _cached;

        /// <summary>Delegates to UProfilerHost.enableFunctionAnalysis (single switch for function hook).</summary>
        public static bool IsFunctionHookEnabled
        {
            get
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                return UProfilerHost.IsFunctionAnalysisEnabled;
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
#endif
