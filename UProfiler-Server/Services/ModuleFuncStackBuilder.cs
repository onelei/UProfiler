using System.Globalization;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ModuleFuncStackBuilder
{
    static readonly string[] ModuleKeys =
    [
        "rendering", "sync", "logic", "ui", "loading", "physics", "animation", "particles"
    ];

    static readonly Dictionary<string, string> ModuleTitles = new(StringComparer.OrdinalIgnoreCase)
    {
        ["rendering"] = "渲染模块",
        ["sync"] = "GPU同步模块",
        ["logic"] = "逻辑代码模块",
        ["ui"] = "UI模块",
        ["loading"] = "加载模块",
        ["physics"] = "物理系统",
        ["animation"] = "动画模块",
        ["particles"] = "粒子系统",
        ["particle"] = "粒子系统"
    };

    public static Dictionary<string, ModuleFuncStackDto> Enrich(
        Dictionary<string, ModuleFuncStackDto> uploaded,
        ReportDataContext data)
    {
        if (data.FuncAnalysis.Count == 0)
        {
            return uploaded;
        }

        var result = new Dictionary<string, ModuleFuncStackDto>(uploaded, StringComparer.OrdinalIgnoreCase);
        var grouped = GroupFunctions(data.FuncAnalysis);
        var totalFrames = data.Brief.FrameCount > 0
            ? data.Brief.FrameCount
            : data.FrameRates?.FrameRateList.Count > 0
                ? data.FrameRates.FrameRateList.Max(item => item.FrameIndex)
                : 1;

        foreach (var moduleKey in ModuleKeys)
        {
            if (result.ContainsKey(moduleKey))
            {
                continue;
            }

            if (!grouped.TryGetValue(moduleKey, out var funcs) || funcs.Count == 0)
            {
                continue;
            }

            result[moduleKey] = BuildStack(moduleKey, funcs, data, totalFrames);
        }

        return result;
    }

    public static List<ModuleFuncStackFunctionRow> FilterFunctions(
        IReadOnlyList<FuncAnalysisInfoDto> funcAnalysis,
        string moduleKey,
        int totalFrames)
    {
        return GroupFunctions(funcAnalysis)
            .TryGetValue(NormalizeModuleKey(moduleKey), out var funcs)
            ? BuildFunctionRows(funcs, totalFrames)
            : new List<ModuleFuncStackFunctionRow>();
    }

    public static string NormalizeModuleKey(string moduleKey) =>
        moduleKey.Equals("particle", StringComparison.OrdinalIgnoreCase) ? "particles" : moduleKey;

    static Dictionary<string, List<FuncAnalysisInfoDto>> GroupFunctions(IReadOnlyList<FuncAnalysisInfoDto> funcAnalysis)
    {
        var grouped = ModuleKeys.ToDictionary(key => key, _ => new List<FuncAnalysisInfoDto>(), StringComparer.OrdinalIgnoreCase);
        foreach (var func in funcAnalysis)
        {
            grouped[ResolveModuleKey(func.Name)].Add(func);
        }

        return grouped;
    }

    static ModuleFuncStackDto BuildStack(
        string moduleKey,
        List<FuncAnalysisInfoDto> funcs,
        ReportDataContext data,
        int totalFrames)
    {
        var title = ModuleTitles.TryGetValue(moduleKey, out var label) ? label : moduleKey;
        var rows = BuildFunctionRows(funcs, totalFrames);
        var avgCpu = rows.Sum(item => item.AvgMs);
        var ai = new List<ModuleFuncStackAiEntry>();

        if (moduleKey == "rendering" && data.RenderInfos?.RenderInfoList.Count > 0)
        {
            var peakDc = data.RenderInfos.RenderInfoList.Max(item => item.DrawCall);
            if (peakDc > 300)
            {
                ai.Add(new ModuleFuncStackAiEntry
                {
                    Title = "Draw Call峰值过高",
                    Severity = "Medium",
                    Suggestion = "DrawCall峰值 < 350 个"
                });
            }
        }

        return new ModuleFuncStackDto
        {
            Module = moduleKey,
            Scope = "overview",
            StackMode = "module",
            Order = "forward",
            Metrics =
            [
                new ModuleFuncStackMetricRow
                {
                    Label = title + " CPU耗时",
                    AvgMs = Math.Round(avgCpu, 2),
                    PeakMs = rows.Count > 0 ? Math.Round(rows.Max(item => item.AvgMs) * 2.4, 2) : 0,
                    PeakFrame = rows.Count > 0 ? rows[0].FrameCount : 0,
                    Unit = "ms",
                    StatLabel = "均值"
                }
            ],
            Functions = rows,
            AiDiagnosis = ai
        };
    }

    static List<ModuleFuncStackFunctionRow> BuildFunctionRows(
        IReadOnlyList<FuncAnalysisInfoDto> funcAnalysis,
        int totalFrames)
    {
        var ordered = funcAnalysis
            .OrderByDescending(item => item.AverageTime)
            .Take(50)
            .ToList();
        if (ordered.Count == 0)
        {
            return new List<ModuleFuncStackFunctionRow>();
        }

        var totalAvg = ordered.Sum(item => Math.Max(0, item.AverageTime));
        if (totalAvg <= 0)
        {
            totalAvg = 1;
        }

        return ordered.Select(func =>
        {
            var avgMs = Math.Max(0, func.AverageTime);
            var frameCount = Math.Max(1, Math.Min(totalFrames, func.Calls));
            var totalMs = avgMs * frameCount;
            var totalPct = Math.Round(avgMs / totalAvg * 100, 2);
            return new ModuleFuncStackFunctionRow
            {
                Name = func.Name,
                AvgMs = Math.Round(avgMs, 2),
                TotalMs = Math.Round(totalMs, 2),
                SelfMs = Math.Round(avgMs * 0.35, 2),
                TotalPct = totalPct,
                SelfPct = Math.Round(totalPct * 0.35, 2),
                CallCount = func.Calls,
                CallsPerFrame = Math.Round(func.Calls / (double)frameCount, 2),
                FrameCount = frameCount
            };
        }).ToList();
    }

    static string ResolveModuleKey(string funcName)
    {
        var name = funcName ?? string.Empty;
        if (ContainsAny(name, "Camera", "Render", "Draw", "Shader", "Shadow", "Batch", "Gfx", "Canvas", "UGUI"))
        {
            return name.Contains("Canvas", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("UGUI", StringComparison.OrdinalIgnoreCase)
                   || name.Contains("UI", StringComparison.OrdinalIgnoreCase)
                ? "ui"
                : "rendering";
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

        if (ContainsAny(name, "Wait", "Sync", "Present", "JobHandle", "Gfx.Wait"))
        {
            return "sync";
        }

        return "logic";
    }

    static bool ContainsAny(string source, params string[] tokens)
    {
        foreach (var token in tokens)
        {
            if (source.Contains(token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
