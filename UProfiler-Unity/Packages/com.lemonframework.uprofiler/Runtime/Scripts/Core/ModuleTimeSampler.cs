using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_2020_1_OR_NEWER
using Unity.Profiling;
#endif

namespace LemonFramework.UProfiler.Core
{
    /// <summary>Samples Unity Profiler module categories at interval frames for moduleTime_ upload.</summary>
    public sealed class ModuleTimeSampler : IDisposable
    {
        static readonly (string key, string label, string color, double recommendMs)[] ModuleDefs =
        {
            ("logic", "逻辑代码", "#1677ff", 2.0),
            ("ui", "UI", "#eb2f96", 2.0),
            ("rendering", "渲染", "#fa8c16", 3.0),
            ("animation", "动画", "#722ed1", 1.0),
            ("sync", "同步等待", "#13c2c2", 1.0),
            ("particles", "粒子系统", "#fadb14", 0.5),
            ("loading", "加载", "#52c41a", 1.0),
            ("physics", "物理", "#9254de", 1.0),
            ("overhead", "Overhead", "#bfbfbf", 1.0)
        };

#if UNITY_2020_1_OR_NEWER
        static readonly (string key, ProfilerCategory category, string[] markers)[] RecorderDefs =
        {
            ("logic", ProfilerCategory.Scripts, new[] { "Update.ScriptRunBehaviourUpdate", "BehaviourUpdate" }),
            ("rendering", ProfilerCategory.Render, new[] { "Camera.Render", "Render.OpaqueGeometry" }),
            ("ui", ProfilerCategory.Render, new[] { "Canvas.SendWillRenderCanvases", "UGUI.Rendering" }),
            ("physics", ProfilerCategory.Physics, new[] { "Physics.Processing", "Physics.Simulate" }),
            ("animation", ProfilerCategory.Animation, new[] { "Animator.Update", "Animation.Update" }),
            ("particles", ProfilerCategory.Particles, new[] { "ParticleSystem.Update", "ParticleSystem.Draw" }),
            ("loading", ProfilerCategory.Loading, new[] { "Loading.UpdatePreloading", "Loading.ReadObject" }),
            ("sync", ProfilerCategory.Internal, new[] { "WaitForTargetFPS", "Gfx.WaitForTargetAndPresent", "WaitForJobGroupID" })
        };

        readonly List<(string key, ProfilerRecorder recorder)> _recorders = new List<(string, ProfilerRecorder)>();
        readonly Dictionary<string, string> _activeMarkerNames = new Dictionary<string, string>();
        readonly ProfilerRecorder _playerLoopRecorder;
#endif

        public ModuleTimeUploadData Data { get; } = new ModuleTimeUploadData();
        public bool IsSupported { get; private set; }

        public ModuleTimeSampler()
        {
            foreach (var def in ModuleDefs)
            {
                Data.series[def.key] = new List<double>();
            }

#if UNITY_2020_1_OR_NEWER
            foreach (var def in RecorderDefs)
            {
                TryAddRecorder(def.key, def.category, def.markers);
            }

            _playerLoopRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "PlayerLoop");
            IsSupported = _recorders.Count > 0;
#else
            IsSupported = false;
#endif
        }

#if UNITY_2020_1_OR_NEWER
        static void TryAddRecorder(
            string key,
            ProfilerCategory category,
            string[] markers,
            List<(string key, ProfilerRecorder recorder)> recorders,
            Dictionary<string, string> activeMarkerNames)
        {
            foreach (var marker in markers)
            {
                var recorder = ProfilerRecorder.StartNew(category, marker);
                if (recorder.Valid)
                {
                    recorders.Add((key, recorder));
                    activeMarkerNames[key] = marker;
                    return;
                }
            }
        }

        void TryAddRecorder(string key, ProfilerCategory category, string[] markers)
        {
            TryAddRecorder(key, category, markers, _recorders, _activeMarkerNames);
        }

        static double RecorderMs(ProfilerRecorder recorder)
        {
            if (!recorder.Valid)
            {
                return 0;
            }

            return recorder.LastValue / 1_000_000.0;
        }
#endif

        public List<FuncAnalysisInfo> BuildSyntheticFuncAnalysis()
        {
            var list = new List<FuncAnalysisInfo>();
#if UNITY_2020_1_OR_NEWER
            foreach (var (key, _) in _recorders)
            {
                if (!Data.series.TryGetValue(key, out var values) || values.Count == 0)
                {
                    continue;
                }

                var avgMs = values.Average();
                if (avgMs <= 0.001)
                {
                    continue;
                }

                _activeMarkerNames.TryGetValue(key, out var markerName);
                list.Add(new FuncAnalysisInfo
                {
                    name = string.IsNullOrEmpty(markerName) ? key : markerName,
                    averageTime = (float)Math.Round(avgMs, 2),
                    useTime = (float)Math.Round(avgMs * values.Count / 1000.0, 4),
                    calls = values.Count,
                    memory = 0,
                    averageMemory = 0
                });
            }
#endif
            return list;
        }

        public void Sample(int frameIndex)
        {
#if UNITY_2020_1_OR_NEWER
            if (!IsSupported)
            {
                return;
            }

            Data.x.Add(frameIndex);
            var sampled = new Dictionary<string, double>();
            var sum = 0.0;
            foreach (var (key, recorder) in _recorders)
            {
                var ms = RecorderMs(recorder);
                sampled[key] = ms;
                sum += ms;
            }

            var playerLoopMs = RecorderMs(_playerLoopRecorder);
            if (playerLoopMs > 0 && sum > 0 && playerLoopMs > sum)
            {
                sampled["overhead"] = playerLoopMs - sum;
            }
            else
            {
                sampled["overhead"] = 0;
            }

            foreach (var def in ModuleDefs)
            {
                var ms = sampled.TryGetValue(def.key, out var value) ? value : 0;
                Data.series[def.key].Add(Math.Round(ms, 2));
            }
#endif
        }

        public void FinalizeSummary()
        {
            Data.modules.Clear();
            Data.summary.Clear();
            foreach (var def in ModuleDefs)
            {
                Data.modules.Add(new ModuleMetaRow
                {
                    key = def.key,
                    label = def.label,
                    color = def.color,
                    recommendMs = def.recommendMs
                });

                if (!Data.series.TryGetValue(def.key, out var values) || values.Count == 0)
                {
                    continue;
                }

                var avg = values.Average();
                Data.summary.Add(new ModuleSummaryUploadRow
                {
                    key = def.key,
                    label = def.label,
                    color = def.color,
                    averageMs = Math.Round(avg, 2),
                    recommendMs = def.recommendMs,
                    overRecommend = avg > def.recommendMs
                });
            }
        }

        public void Dispose()
        {
#if UNITY_2020_1_OR_NEWER
            foreach (var (_, recorder) in _recorders)
            {
                if (recorder.Valid)
                {
                    recorder.Dispose();
                }
            }

            if (_playerLoopRecorder.Valid)
            {
                _playerLoopRecorder.Dispose();
            }
#endif
        }
    }
}
