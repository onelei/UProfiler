using System.Globalization;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ThreadStackBuilder
{
    static readonly string[] DefaultThreadNames =
    [
        "MainThread",
        "Render Thread",
        "Loading.AsyncRead",
        "Audio Stream Thread",
        "Audio Mixer Thread"
    ];

    public static ThreadStackPayload Build(ReportDataContext data)
    {
        if (data.ThreadStack.Threads.Count > 0)
        {
            return data.ThreadStack;
        }

        var moduleSummary = data.ModuleTime.Summary;
        var threads = new List<ThreadStackThreadRow>();
        foreach (var name in DefaultThreadNames)
        {
            var avgMs = name switch
            {
                "MainThread" => moduleSummary.Sum(item => item.AverageMs),
                "Render Thread" => moduleSummary.FirstOrDefault(item => item.Key == "rendering")?.AverageMs ?? 0,
                "Loading.AsyncRead" => moduleSummary.FirstOrDefault(item => item.Key == "loading")?.AverageMs ?? 0,
                _ => 0
            };

            var functions = name == "MainThread"
                ? data.FuncAnalysis.Take(50).Select(MapFunc).ToList()
                : new List<ThreadStackFunctionRow>();

            threads.Add(new ThreadStackThreadRow
            {
                Name = name,
                AvgCpuMs = Math.Round(avgMs, 2),
                Functions = functions
            });
        }

        return new ThreadStackPayload { Threads = threads };
    }

    static ThreadStackFunctionRow MapFunc(FuncAnalysisInfoDto func)
    {
        var totalMs = func.UseTime * 1000;
        return new ThreadStackFunctionRow
        {
            Name = func.Name,
            AvgMs = Math.Round(func.AverageTime, 2),
            TotalMs = Math.Round(totalMs, 2),
            SelfMs = Math.Round(func.AverageTime * 0.15, 2),
            TotalPct = 0,
            SelfPct = 0,
            CallCount = func.Calls,
            CallsPerFrame = func.Calls > 0 ? Math.Round(func.Calls / Math.Max(1.0, func.Calls / 30.0), 2) : 0,
            FrameCount = func.Calls
        };
    }
}
