using System.Globalization;
using System.Text.Json;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public sealed class ProjectCatalog
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    readonly UploadIndex uploadIndex;
    readonly string reportDir;
    PortalCatalog? cachedCatalog;

    public ProjectCatalog(UploadIndex uploadIndex, string reportDir)
    {
        this.uploadIndex = uploadIndex;
        this.reportDir = reportDir;
    }

    public void InvalidateCache()
    {
        cachedCatalog = null;
    }

    public PortalCatalog Build()
    {
        if (cachedCatalog != null)
        {
            return cachedCatalog;
        }

        var sessions = uploadIndex.GetAllSessionKeys()
            .Select(LoadSessionSummary)
            .Where(item => item != null)
            .Cast<SessionSummary>()
            .OrderByDescending(item => item.SessionKey, StringComparer.Ordinal)
            .ToList();

        var projects = sessions
            .GroupBy(item => string.IsNullOrWhiteSpace(item.PackageName) ? item.ProductName : item.PackageName, StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var ordered = group.OrderByDescending(item => item.SessionKey, StringComparer.Ordinal).ToList();
                var latest = ordered[0];
                return new ProjectSummary
                {
                    PackageName = group.Key,
                    ProductName = latest.ProductName,
                    Platform = latest.Platform,
                    ReportCount = ordered.Count,
                    LatestSession = latest,
                    Sessions = ordered
                };
            })
            .OrderByDescending(item => item.LatestSession?.SessionKey, StringComparer.Ordinal)
            .ToList();

        cachedCatalog = new PortalCatalog
        {
            Projects = projects,
            RecentSessions = sessions.Take(20).ToList()
        };

        return cachedCatalog;
    }

    public ProjectSummary? FindProject(string packageName)
    {
        return Build().Projects.FirstOrDefault(item =>
            string.Equals(item.PackageName, packageName, StringComparison.OrdinalIgnoreCase));
    }

    SessionSummary? LoadSessionSummary(string sessionKey)
    {
        var files = uploadIndex.GetSessionFiles(sessionKey);
        if (files.Count == 0)
        {
            return null;
        }

        var testInfo = ReadJson<TestInfoDto>(files, "test");
        var deviceInfo = ReadJson<DeviceInfoDto>(files, "device");
        var frameRates = ReadJson<FrameRatesDto>(files, "frameRate");
        var renderInfos = ReadJson<RenderInfosDto>(files, "renderInfo");

        var fpsValues = frameRates?.FrameRateList.Select(item => item.Frame).ToList() ?? new List<int>();
        var avgFps = fpsValues.Count > 0 ? fpsValues.Average() : 0;
        var minFps = fpsValues.Count > 0 ? fpsValues.Min() : 0;
        var maxFps = fpsValues.Count > 0 ? fpsValues.Max() : 0;
        var renderList = renderInfos?.RenderInfoList ?? new List<RenderInfoDto>();
        var peakDc = renderList.Count > 0 ? renderList.Max(item => item.DrawCall) : 0;

        var context = ReportDataLoader.Load(sessionKey, testInfo?.PackageName, files);
        var score = ComputeScore(avgFps, minFps, context.DiagnosisItems.Count(item => item.Severity == "HIGH"));

        var reportPath = Path.Combine(reportDir, $"report_{sessionKey}.html");
        DateTime? uploadedAt = File.Exists(reportPath) ? File.GetLastWriteTime(reportPath) : null;
        if (uploadedAt == null)
        {
            var firstFile = files.MinBy(item => File.Exists(item.SavedPath) ? File.GetLastWriteTime(item.SavedPath) : DateTime.MaxValue);
            if (firstFile != null && File.Exists(firstFile.SavedPath))
            {
                uploadedAt = File.GetLastWriteTime(firstFile.SavedPath);
            }
        }

        return new SessionSummary
        {
            SessionKey = sessionKey,
            ProductName = testInfo?.ProductName ?? "未命名项目",
            PackageName = testInfo?.PackageName ?? "",
            Platform = testInfo?.Platform ?? deviceInfo?.OperatingSystem ?? "-",
            Version = testInfo?.Version ?? "-",
            TestTime = testInfo?.TestTime ?? "-",
            DeviceModel = deviceInfo?.DeviceModel ?? deviceInfo?.DeviceName ?? "-",
            AvgFps = avgFps,
            MinFps = minFps,
            MaxFps = maxFps,
            PeakDrawCall = peakDc,
            DiagnosisHigh = context.DiagnosisItems.Count(item => item.Severity == "HIGH"),
            DiagnosisMedium = context.DiagnosisItems.Count(item => item.Severity == "MEDIUM"),
            UploadedAt = uploadedAt,
            Grade = ScoreToGrade(score),
            Score = score
        };
    }

    static double ComputeScore(double avgFps, int minFps, int highIssues)
    {
        var fpsScore = Math.Min(100, avgFps / 60.0 * 100);
        var minScore = Math.Min(100, minFps / 30.0 * 100);
        var issuePenalty = highIssues * 8;
        return Math.Max(0, Math.Min(100, fpsScore * 0.6 + minScore * 0.3 - issuePenalty));
    }

    static string ScoreToGrade(double score)
    {
        if (score >= 85)
        {
            return "A";
        }

        if (score >= 70)
        {
            return "B";
        }

        if (score >= 55)
        {
            return "C";
        }

        return "D";
    }

    static T? ReadJson<T>(IReadOnlyList<SessionUpload> files, string prefix) where T : class
    {
        var file = files.FirstOrDefault(item =>
            item.Prefix.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            && item.OriginalName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

        if (file == null || !File.Exists(file.SavedPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(file.SavedPath), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    public static string EncodePackage(string packageName)
        => Uri.EscapeDataString(packageName);

    public static string DecodePackage(string encoded)
        => Uri.UnescapeDataString(encoded);
}
