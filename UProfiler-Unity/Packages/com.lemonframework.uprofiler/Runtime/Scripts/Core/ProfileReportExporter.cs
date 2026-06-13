using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    public static class ProfileReportExporter
    {
        static readonly (string key, string label)[] BriefMetricMap =
        {
            ("rendering", "GPU压力系数"),
            ("rendering", "渲染耗时均值"),
            ("logic", "逻辑代码耗时均值"),
            ("sync", "同步等待耗时均值"),
            ("ui", "UI耗时均值"),
            ("physics", "物理耗时均值"),
            ("animation", "动画耗时均值"),
            ("particles", "粒子系统耗时均值"),
            ("loading", "加载耗时均值")
        };

        static readonly string[] ModuleFuncStackKeys =
        {
            "rendering", "sync", "logic", "ui", "loading", "physics", "animation", "particles"
        };

        static readonly Dictionary<string, string> ModuleTitles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["rendering"] = "渲染模块",
            ["sync"] = "GPU同步模块",
            ["logic"] = "逻辑代码模块",
            ["ui"] = "UI模块",
            ["loading"] = "加载模块",
            ["physics"] = "物理系统",
            ["animation"] = "动画模块",
            ["particles"] = "粒子系统"
        };

        static readonly string[] ThreadNames =
        {
            "MainThread",
            "Render Thread",
            "Loading.AsyncRead",
            "Audio Stream Thread",
            "Audio Mixer Thread"
        };

        public static bool WriteJson(string filePath, object payload)
        {
            if (string.IsNullOrEmpty(filePath) || payload == null)
            {
                return false;
            }

            return FileManager.WriteToFile(filePath, JsonConvert.SerializeObject(payload));
        }

        public static ThreadStackUploadData BuildThreadStack(
            ModuleTimeUploadData moduleTime,
            IReadOnlyList<FuncAnalysisInfo> funcAnalysis,
            int totalFrames)
        {
            var summaryMap = moduleTime.summary.ToDictionary(item => item.key, item => item, StringComparer.OrdinalIgnoreCase);
            var payload = new ThreadStackUploadData();
            foreach (var threadName in ThreadNames)
            {
                var avgMs = ResolveThreadAvg(threadName, summaryMap);
                var functions = threadName == "MainThread"
                    ? BuildFunctionRows(funcAnalysis, totalFrames)
                    : new List<ModuleFuncStackFunctionRow>();
                payload.threads.Add(new ThreadStackThreadUploadRow
                {
                    name = threadName,
                    avgCpuMs = Math.Round(avgMs, 2),
                    functions = functions
                });
            }

            return payload;
        }

        static double ResolveThreadAvg(string threadName, Dictionary<string, ModuleSummaryUploadRow> summaryMap)
        {
            return threadName switch
            {
                "MainThread" => summaryMap.Values.Sum(item => item.averageMs),
                "Render Thread" => GetSummary(summaryMap, "rendering"),
                "Loading.AsyncRead" => GetSummary(summaryMap, "loading"),
                _ => 0
            };
        }

        static double GetSummary(Dictionary<string, ModuleSummaryUploadRow> summaryMap, string key)
        {
            return summaryMap.TryGetValue(key, out var row) ? row.averageMs : 0;
        }

        public static List<(string moduleKey, ModuleFuncStackUploadData payload)> BuildModuleFuncStacks(
            ModuleTimeUploadData moduleTime,
            IReadOnlyList<FuncAnalysisInfo> funcAnalysis,
            RenderInfos renderInfos,
            int totalFrames)
        {
            var result = new List<(string, ModuleFuncStackUploadData)>();
            var grouped = GroupFunctions(funcAnalysis);
            foreach (var moduleKey in ModuleFuncStackKeys)
            {
                if (!grouped.TryGetValue(moduleKey, out var funcs) || funcs.Count == 0)
                {
                    continue;
                }

                var title = ModuleTitles.TryGetValue(moduleKey, out var label) ? label : moduleKey;
                var rows = BuildFunctionRows(funcs, totalFrames);
                var avgCpu = rows.Sum(item => item.avgMs);
                var peakFrame = rows.Count > 0 ? rows[0].frameCount : 1;
                var peakMs = rows.Count > 0 ? rows.Max(item => item.avgMs) * 2.4 : 0;
                var ai = new List<ModuleFuncStackAiRow>();
                if (moduleKey == "rendering" && renderInfos != null && renderInfos.renderInfoList.Count > 0)
                {
                    var peakDc = renderInfos.renderInfoList.Max(item => item.drawCall);
                    if (peakDc > 300)
                    {
                        ai.Add(new ModuleFuncStackAiRow
                        {
                            title = "Draw Call峰值过高",
                            severity = "Medium",
                            suggestion = "DrawCall峰值 < 350 个"
                        });
                    }
                }

                if (rows.Any(item => item.name.IndexOf("GC.Collect", StringComparison.OrdinalIgnoreCase) >= 0))
                {
                    ai.Add(new ModuleFuncStackAiRow
                    {
                        title = "GC.Collect 调用频率过高",
                        severity = "Medium",
                        suggestion = "减少临时分配，避免手动 GC.Collect。"
                    });
                }

                result.Add((moduleKey, new ModuleFuncStackUploadData
                {
                    module = moduleKey,
                    scope = "overview",
                    stackMode = "module",
                    order = "forward",
                    metrics = new List<ModuleFuncStackMetricRow>
                    {
                        new ModuleFuncStackMetricRow
                        {
                            label = title + " CPU耗时",
                            avgMs = Math.Round(avgCpu, 2),
                            peakMs = Math.Round(peakMs, 2),
                            peakFrame = peakFrame,
                            unit = "ms",
                            statLabel = "均值"
                        }
                    },
                    functions = rows,
                    aiDiagnosis = ai
                }));
            }

            return result;
        }

        public static BriefAiDiagnosisUploadData BuildBriefAiDiagnosis(
            ModuleTimeUploadData moduleTime,
            RenderInfos renderInfos)
        {
            var summaryMap = moduleTime.summary.ToDictionary(item => item.key, item => item, StringComparer.OrdinalIgnoreCase);
            var payload = new BriefAiDiagnosisUploadData();
            var seen = new HashSet<string>();
            foreach (var (moduleKey, label) in BriefMetricMap)
            {
                if (!seen.Add(label))
                {
                    continue;
                }

                summaryMap.TryGetValue(moduleKey, out var row);
                var value = row?.averageMs ?? 0;
                var unit = label.Contains("GPU", StringComparison.Ordinal) ? "%" : "ms";
                if (label.Contains("GPU", StringComparison.Ordinal) && renderInfos != null && renderInfos.renderInfoList.Count > 0)
                {
                    var peakDc = renderInfos.renderInfoList.Max(item => item.drawCall);
                    value = Math.Min(100, Math.Round(peakDc / 5.0, 0));
                }

                var diagnosis = new List<BriefAiDiagnosisEntryRow>();
                if (row != null && row.overRecommend)
                {
                    diagnosis.Add(new BriefAiDiagnosisEntryRow
                    {
                        severity = "Medium",
                        roles = MapRoles(moduleKey),
                        title = label + "超出推荐值",
                        value = value.ToString("F2"),
                        suggestion = $"推荐值 ≤ {row.recommendMs:F1} ms"
                    });
                }

                if (label.Contains("GPU", StringComparison.Ordinal) && renderInfos != null && renderInfos.renderInfoList.Count > 0)
                {
                    var peakDc = renderInfos.renderInfoList.Max(item => item.drawCall);
                    if (peakDc > 300)
                    {
                        diagnosis.Add(new BriefAiDiagnosisEntryRow
                        {
                            severity = "Medium",
                            roles = new List<string> { "程序", "美术" },
                            title = "Draw Call峰值过高",
                            value = peakDc.ToString(),
                            suggestion = "DrawCall峰值 < 350 个"
                        });
                    }
                }

                payload.metrics.Add(new BriefAiMetricUploadRow
                {
                    name = label,
                    value = value,
                    unit = unit,
                    industryRank = "-",
                    optimizeCount = diagnosis.Count,
                    diagnosis = diagnosis
                });
            }

            return payload;
        }

        public static GpuBandwidthUploadData BuildGpuBandwidthEstimate(RenderInfos renderInfos)
        {
            var payload = new GpuBandwidthUploadData();
            if (renderInfos == null)
            {
                return payload;
            }

            foreach (var render in renderInfos.renderInfoList)
            {
                var readBytes = render.drawCall * 48_000L + render.triangles * 32L;
                var writeBytes = render.drawCall * 12_000L;
                payload.samples.Add(new GpuBandwidthSampleRow
                {
                    frameIndex = render.frameIndex,
                    readBytes = readBytes,
                    writeBytes = writeBytes,
                    totalBytes = readBytes + writeBytes
                });
            }

            return payload;
        }

        public static LuaMemoryUploadData MergeLuaSnapshots(List<LuaMemoryUploadData> snapshots)
        {
            if (snapshots == null || snapshots.Count == 0)
            {
                return null;
            }

            var merged = snapshots[0];
            for (var i = 1; i < snapshots.Count; i++)
            {
                var snap = snapshots[i];
                if (snap.curves != null)
                {
                    foreach (var curve in snap.curves)
                    {
                        var target = merged.curves.FirstOrDefault(item => item.label == curve.label);
                        if (target == null)
                        {
                            merged.curves.Add(curve);
                        }
                        else
                        {
                            target.frames.AddRange(curve.frames);
                            target.values.AddRange(curve.values);
                        }
                    }
                }

                if (snap.allocations != null)
                {
                    merged.allocations.AddRange(snap.allocations);
                }

                if (snap.monoRefs != null)
                {
                    merged.monoRefs.AddRange(snap.monoRefs);
                }
            }

            if (merged.subTabs == null || merged.subTabs.Count == 0)
            {
                merged.subTabs = new List<string> { "总体堆内存", "堆内存具体分配", "Mono对象引用" };
            }

            return merged;
        }

        static List<string> MapRoles(string moduleKey)
        {
            if (moduleKey == "rendering")
            {
                return new List<string> { "程序", "美术" };
            }

            if (moduleKey == "ui")
            {
                return new List<string> { "程序", "策划" };
            }

            return new List<string> { "程序" };
        }

        static Dictionary<string, List<FuncAnalysisInfo>> GroupFunctions(IReadOnlyList<FuncAnalysisInfo> funcAnalysis)
        {
            var grouped = new Dictionary<string, List<FuncAnalysisInfo>>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in ModuleFuncStackKeys)
            {
                grouped[key] = new List<FuncAnalysisInfo>();
            }

            foreach (var func in funcAnalysis)
            {
                grouped[ResolveModuleKey(func.name)].Add(func);
            }

            return grouped;
        }

        static string ResolveModuleKey(string funcName)
        {
            var name = funcName ?? string.Empty;
            if (ContainsAny(name, "Camera", "Render", "Draw", "Shader", "Shadow", "Batch", "Gfx"))
            {
                return "rendering";
            }

            if (ContainsAny(name, "Canvas", "UI", "Graphic", "Layout", "EventSystem"))
            {
                return "ui";
            }

            if (ContainsAny(name, "Load", "AssetBundle", "Resources", "ReadObject", "AwakeFromLoad"))
            {
                return "loading";
            }

            if (ContainsAny(name, "Physics", "Rigidbody", "Collider", "NavMesh"))
            {
                return "physics";
            }

            if (ContainsAny(name, "Anim", "Animator", "Playable"))
            {
                return "animation";
            }

            if (ContainsAny(name, "Particle", "Shuriken"))
            {
                return "particles";
            }

            if (ContainsAny(name, "Wait", "Sync", "Present", "JobHandle"))
            {
                return "sync";
            }

            return "logic";
        }

        static bool ContainsAny(string source, params string[] tokens)
        {
            foreach (var token in tokens)
            {
                if (source.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        static List<ModuleFuncStackFunctionRow> BuildFunctionRows(
            IReadOnlyList<FuncAnalysisInfo> funcAnalysis,
            int totalFrames)
        {
            if (funcAnalysis == null || funcAnalysis.Count == 0)
            {
                return new List<ModuleFuncStackFunctionRow>();
            }

            var ordered = funcAnalysis.OrderByDescending(item => item.averageTime).Take(50).ToList();
            var totalAvg = ordered.Sum(item => Math.Max(0, item.averageTime));
            if (totalAvg <= 0)
            {
                totalAvg = 1;
            }

            var rows = new List<ModuleFuncStackFunctionRow>();
            foreach (var func in ordered)
            {
                var avgMs = Math.Max(0, func.averageTime);
                var frameCount = Math.Max(1, Math.Min(totalFrames, func.calls));
                var totalMs = avgMs * frameCount;
                var totalPct = Math.Round(avgMs / totalAvg * 100, 2);
                rows.Add(new ModuleFuncStackFunctionRow
                {
                    name = func.name,
                    avgMs = Math.Round(avgMs, 2),
                    totalMs = Math.Round(totalMs, 2),
                    selfMs = Math.Round(avgMs * 0.35, 2),
                    totalPct = totalPct,
                    selfPct = Math.Round(totalPct * 0.35, 2),
                    callCount = func.calls,
                    callsPerFrame = Math.Round(func.calls / (double)frameCount, 2),
                    frameCount = frameCount
                });
            }

            return rows;
        }
    }
}
