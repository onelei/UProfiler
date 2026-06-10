using System.Globalization;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ReportBriefBuilder
{
    public static PerformanceBriefPayload Build(ReportDataContext data, JankAnalysisPayload jank)
    {
        var moduleSummary = data.ModuleTime.Summary.ToDictionary(item => item.Key, item => item);
        var diagnosisByModule = data.DiagnosisItems
            .GroupBy(item => item.Category)
            .ToDictionary(group => group.Key, group => group.Count());

        var peakMemoryGb = data.PeakPssMb > 0
            ? data.PeakPssMb / 1024.0
            : data.PeakTotalAllocated / 1024.0 / 1024.0 / 1024.0;

        var frameCount = data.FrameRates?.FrameRateList.Count ?? 0;
        var powerPer10k = frameCount > 0 && data.PowerInfos?.DevicePowerConsumeInfos.Count > 0
            ? Math.Round(data.PowerInfos.DevicePowerConsumeInfos.Average(item => item.BatteryPower) * 10000 / frameCount, 2)
            : 0;

        var tempDelta = 0;
        if (data.PowerInfos?.DevicePowerConsumeInfos.Count > 1)
        {
            var temps = data.PowerInfos.DevicePowerConsumeInfos.Select(item => item.CpuTemperate).ToList();
            tempDelta = temps.Max() - temps.Min();
        }

        var kpis = new List<BriefKpiCard>
        {
            new()
            {
                Key = "fps",
                Label = "FPS均值",
                Value = data.AvgFps.ToString("F2", CultureInfo.InvariantCulture),
                Unit = "帧/秒",
                TaskCount = CountTasks(data, "整体")
            },
            new()
            {
                Key = "jank",
                Label = "Jank 均值",
                Value = jank.JankPerMinute.ToString("F2", CultureInfo.InvariantCulture),
                Unit = "次/分钟",
                TaskCount = jank.JankCount > 0 ? Math.Min(jank.JankCount, 99) : 0
            },
            new()
            {
                Key = "memory",
                Label = "设备内存峰值",
                Value = peakMemoryGb.ToString("F2", CultureInfo.InvariantCulture),
                Unit = "GB",
                TaskCount = CountTasks(data, "内存")
            },
            new()
            {
                Key = "power",
                Label = "每万帧耗电均值",
                Value = powerPer10k.ToString("F1", CultureInfo.InvariantCulture),
                Unit = "%",
                TaskCount = CountTasks(data, "硬件参数")
            },
            new()
            {
                Key = "temperature",
                Label = "温度变化量",
                Value = tempDelta.ToString(CultureInfo.InvariantCulture),
                Unit = "℃",
                TaskCount = tempDelta > 15 ? 1 : 0
            }
        };

        var metrics = new List<BriefMetricRow>
        {
            BuildModuleMetric("GPU压力系数", moduleSummary, "rendering", "%", data.PeakDrawCall > 200),
            BuildModuleMetric("渲染耗时均值", moduleSummary, "rendering", "ms"),
            BuildModuleMetric("逻辑代码耗时均值", moduleSummary, "logic", "ms"),
            BuildModuleMetric("同步等待耗时均值", moduleSummary, "sync", "ms"),
            BuildModuleMetric("UI耗时均值", moduleSummary, "ui", "ms"),
            BuildModuleMetric("物理耗时均值", moduleSummary, "physics", "ms"),
            BuildModuleMetric("动画耗时均值", moduleSummary, "animation", "ms"),
            BuildModuleMetric("粒子系统耗时均值", moduleSummary, "particles", "ms"),
            BuildModuleMetric("加载耗时均值", moduleSummary, "loading", "ms")
        };

        var optimizable = metrics.Count(item => item.TaskCount > 0);
        return new PerformanceBriefPayload
        {
            FrameCount = frameCount,
            Kpis = kpis,
            Metrics = metrics,
            OptimizableCount = optimizable,
            TotalMetricCount = metrics.Count,
            SummaryText = data.AvgFps >= 45 ? "游戏运行时帧率较为合理" : "游戏运行时帧率偏低，建议优化"
        };
    }

    static int CountTasks(ReportDataContext data, string category)
        => data.DiagnosisItems.Count(item =>
            item.Category.Equals(category, StringComparison.OrdinalIgnoreCase)
            && item.Severity is "HIGH" or "MEDIUM");

    static BriefMetricRow BuildModuleMetric(
        string label,
        Dictionary<string, ModuleSummaryRow> summary,
        string key,
        string unit,
        bool forceTask = false)
    {
        summary.TryGetValue(key, out var row);
        var value = row?.AverageMs ?? 0;
        if (label == "GPU压力系数")
        {
            value = Math.Min(100, Math.Round(value / 16.67 * 100, 0));
        }

        return new BriefMetricRow
        {
            Label = label,
            Value = value.ToString(label == "GPU压力系数" ? "F0" : "F2", CultureInfo.InvariantCulture),
            Unit = unit,
            TaskCount = forceTask || row?.OverRecommend == true ? 1 : 0
        };
    }
}
