using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class JankAnalyzer
{
    public static JankAnalysisPayload Build(ReportDataContext data)
    {
        var fpsList = data.FrameRates?.FrameRateList ?? new List<UProfilerFrameInfoDto>();
        if (fpsList.Count < 2)
        {
            return new JankAnalysisPayload();
        }

        var jankFrames = new List<JankFrameRow>();
        var prevFps = fpsList[0].Frame;
        for (var i = 1; i < fpsList.Count; i++)
        {
            var current = fpsList[i];
            var fps = Math.Max(1, current.Frame);
            var frameMs = 1000.0 / fps;
            var prevMs = 1000.0 / Math.Max(1, prevFps);
            var isBigJank = frameMs > prevMs * 2 && frameMs > 83.3;
            var isJank = frameMs > prevMs * 2 && frameMs > 41.6;
            if (isJank || isBigJank)
            {
                jankFrames.Add(new JankFrameRow
                {
                    FrameIndex = current.FrameIndex,
                    Fps = fps,
                    FrameMs = Math.Round(frameMs, 2),
                    JankType = isBigJank ? "BigJank" : "Jank"
                });
            }

            prevFps = fps;
        }

        var testMinutes = ParseTestMinutes(data.TestInfo?.TestTime);
        var jankPerMinute = testMinutes > 0
            ? Math.Round(jankFrames.Count / testMinutes, 2)
            : 0;

        var hotFuncs = data.FuncAnalysis
            .Where(item => item.AverageTime >= 8f)
            .OrderByDescending(item => item.AverageTime)
            .Take(20)
            .Select(item => new JankFunctionRow
            {
                Name = item.Name,
                AverageMs = Math.Round(item.AverageTime, 2),
                Calls = item.Calls,
                TotalSeconds = Math.Round(item.UseTime, 3)
            })
            .ToList();

        var jankHotFunctions = BuildJankHotFunctions(data.FuncAnalysis, jankFrames.Count);
        var severeCount = jankFrames.Count(item => item.JankType == "BigJank");
        var loadingCount = jankHotFunctions.Count(item =>
            item.Name.Contains("Load", StringComparison.OrdinalIgnoreCase)
            || item.Name.Contains("Preload", StringComparison.OrdinalIgnoreCase));
        var otherCount = Math.Max(0, jankFrames.Count - loadingCount);

        return new JankAnalysisPayload
        {
            JankPerMinute = jankPerMinute,
            JankCount = jankFrames.Count,
            BigJankCount = jankFrames.Count(item => item.JankType == "BigJank"),
            SevereJankCount = severeCount,
            LoadingJankCount = Math.Min(loadingCount, jankFrames.Count),
            OtherJankCount = otherCount,
            Frames = jankFrames,
            HotFunctions = hotFuncs,
            JankHotFunctions = jankHotFunctions
        };
    }

    static List<JankHotFunctionRow> BuildJankHotFunctions(
        List<FuncAnalysisInfoDto> funcAnalysis,
        int jankCount)
    {
        if (funcAnalysis.Count == 0 || jankCount == 0)
        {
            return new List<JankHotFunctionRow>();
        }

        var totalMs = funcAnalysis.Sum(item => Math.Max(0, item.UseTime * 1000));
        if (totalMs <= 0)
        {
            totalMs = 1;
        }

        return funcAnalysis
            .Where(item => item.AverageTime >= 5f)
            .OrderByDescending(item => item.UseTime)
            .Take(12)
            .Select(item =>
            {
                var total = item.UseTime * 1000;
                var self = item.AverageTime;
                return new JankHotFunctionRow
                {
                    Name = item.Name,
                    KeyJankCount = item.AverageTime >= 15 ? Math.Min(jankCount, 10) : Math.Min(jankCount, 4),
                    TotalRatio = Math.Round(total / totalMs * 100, 2),
                    SelfRatio = Math.Round(self / Math.Max(1, item.AverageTime + self) * 100, 2),
                    TotalMs = Math.Round(total, 3),
                    SelfMs = Math.Round(self, 3),
                    SpreadJankCount = jankCount
                };
            })
            .ToList();
    }

    static double ParseTestMinutes(string? testTime)
    {
        if (string.IsNullOrWhiteSpace(testTime))
        {
            return 1;
        }

        var minutes = 0;
        var seconds = 0;
        var minuteMatch = System.Text.RegularExpressions.Regex.Match(testTime, @"(\d+)\s*分钟");
        if (minuteMatch.Success)
        {
            minutes = int.Parse(minuteMatch.Groups[1].Value);
        }

        var secondMatch = System.Text.RegularExpressions.Regex.Match(testTime, @"(\d+)\s*秒");
        if (secondMatch.Success)
        {
            seconds = int.Parse(secondMatch.Groups[1].Value);
        }

        var total = minutes + seconds / 60.0;
        return total > 0 ? total : 1;
    }
}
