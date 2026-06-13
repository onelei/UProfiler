using System.Globalization;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ModuleTimeBuilder
{
    static readonly ModuleDefinition[] ModuleDefinitions =
    {
        new("logic", "逻辑代码", 2.0, "#1677ff"),
        new("ui", "UI", 2.0, "#eb2f96"),
        new("rendering", "渲染", 3.0, "#fa8c16"),
        new("animation", "动画", 1.0, "#722ed1"),
        new("sync", "同步等待", 1.0, "#13c2c2"),
        new("particles", "粒子系统", 0.5, "#fadb14"),
        new("loading", "加载", 1.0, "#52c41a"),
        new("physics", "物理", 1.0, "#9254de"),
        new("overhead", "Overhead", 1.0, "#bfbfbf")
    };

    public static ModuleTimePayload Build(ReportDataContext data)
    {
        var fpsList = data.FrameRates?.FrameRateList ?? new List<UProfilerFrameInfoDto>();
        var renderList = data.RenderInfos?.RenderInfoList ?? new List<RenderInfoDto>();
        var monitorList = data.UProfilerInfos?.GetAll() ?? new List<UProfilerInfoDto>();

        if (fpsList.Count == 0)
        {
            return EmptyPayload();
        }

        var sampleFrames = monitorList.Count > 0
            ? monitorList.Select(item => item.FrameIndex).Distinct().OrderBy(item => item).ToList()
            : fpsList
                .Where(item => item.FrameIndex % Math.Max(1, data.TestInfo?.IntervalFrame ?? 100) == 0)
                .Select(item => item.FrameIndex)
                .Distinct()
                .OrderBy(item => item)
                .ToList();

        if (sampleFrames.Count == 0)
        {
            var interval = Math.Max(1, data.TestInfo?.IntervalFrame ?? 100);
            sampleFrames = fpsList
                .Where(item => item.FrameIndex % interval == 0)
                .Select(item => item.FrameIndex)
                .Distinct()
                .OrderBy(item => item)
                .ToList();
        }

        var avgFps = fpsList.Average(item => Math.Max(1, item.Frame));
        var funcLogicMs = Math.Min(
            50,
            data.FuncAnalysis.Sum(item => Math.Max(0, item.AverageTime)));
        var peakDrawCall = renderList.Count > 0 ? Math.Max(1, renderList.Max(item => item.DrawCall)) : 200;
        var peakSetPass = renderList.Count > 0 ? Math.Max(1, renderList.Max(item => item.SetPassCall)) : 200;

        var x = new List<int>();
        var series = ModuleDefinitions.ToDictionary(
            item => item.Key,
            _ => new List<double>());

        foreach (var frameIndex in sampleFrames)
        {
            var fps = LookupFps(fpsList, frameIndex);
            var frameTimeMs = fps > 0 ? 1000.0 / fps : 16.67;
            var render = LookupRender(renderList, frameIndex);
            var memoryDelta = LookupMemoryDelta(monitorList, frameIndex);

            var weights = ModuleDefinitions.ToDictionary(item => item.Key, item => item.BaseWeight);
            weights["logic"] = funcLogicMs > 0 ? funcLogicMs : frameTimeMs * 0.32;
            weights["rendering"] = Math.Max(
                frameTimeMs * 0.12,
                frameTimeMs * 0.35 * ((render.DrawCall / (double)peakDrawCall) + (render.SetPassCall / (double)peakSetPass)) / 2.0);
            weights["sync"] = fps < avgFps * 0.92
                ? frameTimeMs * Math.Min(0.35, (avgFps - fps) / avgFps)
                : frameTimeMs * 0.04;
            weights["loading"] = memoryDelta > 5 * 1024 * 1024
                ? frameTimeMs * 0.18
                : frameTimeMs * 0.03;

            var weightSum = weights.Values.Sum();
            x.Add(frameIndex);
            foreach (var module in ModuleDefinitions)
            {
                var value = frameTimeMs * weights[module.Key] / weightSum;
                series[module.Key].Add(Math.Round(value, 2));
            }
        }

        var averages = ModuleDefinitions.Select(module =>
        {
            var values = series[module.Key];
            var avg = values.Count > 0 ? values.Average() : 0;
            return new ModuleSummaryRow
            {
                Key = module.Key,
                Label = module.Label,
                Color = module.Color,
                AverageMs = Math.Round(avg, 2),
                RecommendMs = module.RecommendMs,
                OverRecommend = avg > module.RecommendMs
            };
        }).ToList();

        return new ModuleTimePayload
        {
            Modules = ModuleDefinitions.Select(item => new ModuleMeta
            {
                Key = item.Key,
                Label = item.Label,
                Color = item.Color,
                RecommendMs = item.RecommendMs
            }).ToList(),
            X = x,
            Series = series,
            Summary = averages
        };
    }

    public static ModuleTimePayload EnrichUploaded(ModuleTimePayload uploaded)
    {
        if (uploaded.Modules.Count > 0 && uploaded.Summary.Count > 0)
        {
            return uploaded;
        }

        var modules = ModuleDefinitions.Select(item => new ModuleMeta
        {
            Key = item.Key,
            Label = item.Label,
            Color = item.Color,
            RecommendMs = item.RecommendMs
        }).ToList();

        var summary = ModuleDefinitions.Select(module =>
        {
            uploaded.Series.TryGetValue(module.Key, out var values);
            values ??= new List<double>();
            var avg = values.Count > 0 ? values.Average() : 0;
            return new ModuleSummaryRow
            {
                Key = module.Key,
                Label = module.Label,
                Color = module.Color,
                AverageMs = Math.Round(avg, 2),
                RecommendMs = module.RecommendMs,
                OverRecommend = avg > module.RecommendMs
            };
        }).ToList();

        return new ModuleTimePayload
        {
            Modules = modules,
            X = uploaded.X,
            Series = uploaded.Series,
            Summary = summary
        };
    }

    static ModuleTimePayload EmptyPayload() => new()
    {
        Modules = ModuleDefinitions.Select(item => new ModuleMeta
        {
            Key = item.Key,
            Label = item.Label,
            Color = item.Color,
            RecommendMs = item.RecommendMs
        }).ToList()
    };

    static int LookupFps(List<UProfilerFrameInfoDto> fpsList, int frameIndex)
    {
        var exact = fpsList.FirstOrDefault(item => item.FrameIndex == frameIndex);
        if (exact != null && exact.Frame > 0)
        {
            return exact.Frame;
        }

        return fpsList
            .Where(item => item.Frame > 0)
            .OrderBy(item => Math.Abs(item.FrameIndex - frameIndex))
            .Select(item => item.Frame)
            .FirstOrDefault();
    }

    static RenderInfoDto LookupRender(List<RenderInfoDto> renderList, int frameIndex)
    {
        if (renderList.Count == 0)
        {
            return new RenderInfoDto();
        }

        return renderList
            .OrderBy(item => Math.Abs(item.FrameIndex - frameIndex))
            .First();
    }

    static long LookupMemoryDelta(List<UProfilerInfoDto> monitorList, int frameIndex)
    {
        if (monitorList.Count < 2)
        {
            return 0;
        }

        var ordered = monitorList.OrderBy(item => item.FrameIndex).ToList();
        var currentIndex = ordered.FindIndex(item => item.FrameIndex == frameIndex);
        if (currentIndex <= 0)
        {
            return 0;
        }

        var current = ordered[currentIndex];
        var previous = ordered[currentIndex - 1];
        return Math.Abs(current.TotalAllocatedMemory - previous.TotalAllocatedMemory);
    }

    sealed record ModuleDefinition(string Key, string Label, double RecommendMs, string Color)
    {
        public double BaseWeight => Key switch
        {
            "logic" => 0.32,
            "ui" => 0.08,
            "rendering" => 0.22,
            "animation" => 0.05,
            "sync" => 0.05,
            "particles" => 0.02,
            "loading" => 0.04,
            "physics" => 0.05,
            "overhead" => 0.17,
            _ => 0.1
        };
    }
}
