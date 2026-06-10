using System.Globalization;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class SceneInfoBuilder
{
    static readonly string[] OverviewModuleOrder =
    [
        "rendering", "ui", "loading", "animation", "particles", "physics", "sync", "logic"
    ];

    public static SceneManagementPayload Build(ReportDataContext data)
    {
        var fpsList = data.FrameRates?.FrameRateList ?? new List<UProfilerFrameInfoDto>();
        var frameTimes = fpsList
            .Where(item => item.Frame > 0)
            .Select(item => new FrameTimePoint
            {
                FrameIndex = item.FrameIndex,
                FrameMs = Math.Round(1000.0 / item.Frame, 2)
            })
            .ToList();

        var segments = data.SceneInfo?.Segments ?? new List<SceneSegmentDto>();
        if (segments.Count == 0 && fpsList.Count > 0)
        {
            segments =
            [
                new SceneSegmentDto
                {
                    SceneName = "默认场景",
                    StartFrame = fpsList.Min(item => item.FrameIndex),
                    EndFrame = fpsList.Max(item => item.FrameIndex),
                    Note = "未上传 sceneInfo_，整段测试合并为单场景"
                }
            ];
        }

        var hasSceneInfo = data.SceneInfo?.Segments.Count > 0;
        var filteredSegments = hasSceneInfo
            ? segments.Where(segment => segment.EndFrame - segment.StartFrame + 1 >= 100).ToList()
            : segments;

        var rows = filteredSegments.Select(segment => BuildSceneRow(segment, data, frameTimes)).ToList();
        var overview = BuildOverviewBars(filteredSegments, data);

        return new SceneManagementPayload
        {
            HasSceneInfo = data.SceneInfo?.Segments.Count > 0,
            FrameTimes = frameTimes,
            Scenes = rows,
            OverviewBars = overview.Bars,
            OverviewModules = overview.ModuleLabels
        };
    }

    static SceneTableRow BuildSceneRow(
        SceneSegmentDto segment,
        ReportDataContext data,
        List<FrameTimePoint> frameTimes)
    {
        var fpsInRange = data.FrameRates?.FrameRateList
            .Where(item => item.FrameIndex >= segment.StartFrame && item.FrameIndex <= segment.EndFrame)
            .ToList() ?? new List<UProfilerFrameInfoDto>();
        var monitorInRange = data.UProfilerInfos?.GetAll()
            .Where(item => item.FrameIndex >= segment.StartFrame && item.FrameIndex <= segment.EndFrame)
            .ToList() ?? new List<UProfilerInfoDto>();
        var renderInRange = data.RenderInfos?.RenderInfoList
            .Where(item => item.FrameIndex >= segment.StartFrame && item.FrameIndex <= segment.EndFrame)
            .ToList() ?? new List<RenderInfoDto>();
        var pssInRange = data.MemoryUseDatas?.MemoryUsedList
            .Where(item => item.FrameIndex >= segment.StartFrame && item.FrameIndex <= segment.EndFrame)
            .ToList() ?? new List<MemoryUseDataDto>();
        var msInRange = frameTimes
            .Where(item => item.FrameIndex >= segment.StartFrame && item.FrameIndex <= segment.EndFrame)
            .Select(item => item.FrameMs)
            .ToList();

        var frameCount = Math.Max(0, segment.EndFrame - segment.StartFrame + 1);
        return new SceneTableRow
        {
            SceneName = segment.SceneName,
            StartFrame = segment.StartFrame,
            EndFrame = segment.EndFrame,
            FrameCount = frameCount,
            AvgFrameMs = msInRange.Count > 0 ? Math.Round(msInRange.Average(), 2) : 0,
            AvgFps = fpsInRange.Count > 0 ? Math.Round(fpsInRange.Average(item => item.Frame), 2) : 0,
            PeakPssMb = pssInRange.Count > 0 ? pssInRange.Max(item => item.PssMemorySize) : 0,
            PeakMonoMb = monitorInRange.Count > 0
                ? Math.Round(monitorInRange.Max(item => item.MonoUsedSize) / 1024.0 / 1024.0, 2)
                : 0,
            PeakCpuMs = msInRange.Count > 0 ? Math.Round(msInRange.Max(), 2) : 0,
            PeakTriangles = renderInRange.Count > 0 ? renderInRange.Max(item => item.Triangles) : 0,
            PeakDrawCall = renderInRange.Count > 0 ? renderInRange.Max(item => item.DrawCall) : 0,
            Note = segment.Note ?? ""
        };
    }

    static (List<SceneOverviewBarRow> Bars, List<string> ModuleLabels) BuildOverviewBars(
        List<SceneSegmentDto> segments,
        ReportDataContext data)
    {
        var moduleTime = data.ModuleTime;
        var moduleLabels = OverviewModuleOrder
            .Select(key => moduleTime.Modules.FirstOrDefault(item => item.Key == key)?.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Select(label => label!)
            .ToList();

        if (moduleTime.X.Count == 0 || moduleTime.Modules.Count == 0)
        {
            return (new List<SceneOverviewBarRow>(), moduleLabels);
        }

        var bars = segments.Select(segment =>
        {
            var moduleMs = new Dictionary<string, double>();
            foreach (var key in OverviewModuleOrder)
            {
                if (!moduleTime.Series.TryGetValue(key, out var values))
                {
                    continue;
                }

                var samples = new List<double>();
                for (var i = 0; i < moduleTime.X.Count && i < values.Count; i++)
                {
                    var frame = moduleTime.X[i];
                    if (frame >= segment.StartFrame && frame <= segment.EndFrame)
                    {
                        samples.Add(values[i]);
                    }
                }

                if (samples.Count > 0)
                {
                    moduleMs[key] = Math.Round(samples.Average(), 2);
                }
            }

            return new SceneOverviewBarRow
            {
                SceneName = segment.SceneName,
                ModuleMs = moduleMs
            };
        }).ToList();

        return (bars, moduleLabels);
    }
}
