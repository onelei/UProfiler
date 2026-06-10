using System.Globalization;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class BriefCollapseBuilder
{
    static readonly (string Label, string ModuleKey, string Category)[] MetricMap =
    [
        ("GPU压力系数", "rendering", "渲染模块"),
        ("渲染耗时均值", "rendering", "渲染模块"),
        ("逻辑代码耗时均值", "logic", "逻辑脚本"),
        ("同步等待耗时均值", "sync", "整体"),
        ("UI耗时均值", "ui", "整体"),
        ("物理耗时均值", "physics", "整体"),
        ("动画耗时均值", "animation", "整体"),
        ("粒子系统耗时均值", "particles", "整体"),
        ("加载耗时均值", "loading", "整体")
    ];

    public static List<BriefMetricRow> Build(ReportDataContext data)
    {
        if (data.BriefAiDiagnosis?.Metrics.Count > 0)
        {
            return data.BriefAiDiagnosis.Metrics.Select(MapAiMetric).ToList();
        }

        var moduleSummary = data.ModuleTime.Summary.ToDictionary(item => item.Key, item => item);
        var metrics = new List<BriefMetricRow>();
        foreach (var (label, moduleKey, category) in MetricMap)
        {
            var baseMetric = data.Brief.Metrics.FirstOrDefault(item => item.Label == label);
            if (baseMetric == null)
            {
                continue;
            }

            var diagnosis = BuildDiagnosis(data, label, moduleKey, category, baseMetric);
            metrics.Add(new BriefMetricRow
            {
                Label = baseMetric.Label,
                Value = baseMetric.Value,
                Unit = baseMetric.Unit,
                TaskCount = baseMetric.TaskCount,
                ModuleKey = moduleKey,
                IndustryRank = diagnosis.Count > 0 ? "待对比" : "-",
                Diagnosis = diagnosis
            });
        }

        return metrics;
    }

    static BriefMetricRow MapAiMetric(BriefAiMetricDto ai)
    {
        return new BriefMetricRow
        {
            Label = ai.Name,
            Value = ai.Value.ToString(ai.Unit == "%" ? "F0" : "F2", CultureInfo.InvariantCulture),
            Unit = ai.Unit,
            TaskCount = ai.OptimizeCount > 0 ? ai.OptimizeCount : ai.Diagnosis.Count,
            IndustryRank = ai.IndustryRank,
            Diagnosis = ai.Diagnosis
        };
    }

    static List<BriefDiagnosisEntry> BuildDiagnosis(
        ReportDataContext data,
        string label,
        string moduleKey,
        string category,
        BriefMetricRow metric)
    {
        var entries = data.DiagnosisItems
            .Where(item =>
                item.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
                || (label.Contains("GPU", StringComparison.Ordinal) && item.Id == "render-dc")
                || (label.Contains("渲染", StringComparison.Ordinal) && item.Category == "渲染模块"))
            .Where(item => item.Severity is "HIGH" or "MEDIUM" || metric.TaskCount > 0)
            .Select(item => new BriefDiagnosisEntry
            {
                Severity = item.Severity == "HIGH" ? "High" : "Medium",
                Roles = MapRoles(item.Category),
                Title = item.Title,
                Value = item.ValueText,
                Suggestion = item.RecommendText
            })
            .ToList();

        if (entries.Count == 0 && metric.TaskCount > 0 && data.ModuleTime.Summary.FirstOrDefault(item => item.Key == moduleKey) is { } row)
        {
            entries.Add(new BriefDiagnosisEntry
            {
                Severity = row.OverRecommend ? "Medium" : "Low",
                Roles = MapRoles(category),
                Title = $"{label}超出推荐值",
                Value = $"{metric.Value} {metric.Unit}",
                Suggestion = $"推荐值 ≤ {row.RecommendMs:F1} ms"
            });
        }

        if (label == "GPU压力系数" && data.PeakDrawCall > 200)
        {
            entries.Insert(0, new BriefDiagnosisEntry
            {
                Severity = data.PeakDrawCall > 350 ? "Medium" : "Low",
                Roles = new List<string> { "程序", "美术" },
                Title = "Draw Call峰值过高",
                Value = data.PeakDrawCall.ToString(CultureInfo.InvariantCulture),
                Suggestion = "DrawCall峰值 < 350 个"
            });
        }

        return entries;
    }

    static List<string> MapRoles(string category) => category switch
    {
        "渲染模块" => new List<string> { "程序", "美术" },
        "逻辑脚本" => new List<string> { "程序", "策划" },
        "内存" => new List<string> { "程序" },
        "硬件参数" => new List<string> { "程序" },
        _ => new List<string> { "程序" }
    };
}
