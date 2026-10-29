using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public sealed class ReportGenerator
{
    readonly UploadIndex uploadIndex;
    readonly string reportDir;

    public ReportGenerator(UploadIndex uploadIndex, string reportDir)
    {
        this.uploadIndex = uploadIndex;
        this.reportDir = reportDir;
        Directory.CreateDirectory(reportDir);
    }

    public string Generate(string sessionKey, string? packageName)
    {
        var files = uploadIndex.GetSessionFiles(sessionKey);
        var context = ReportDataLoader.Load(sessionKey, packageName, files);
        var html = ReportHtmlBuilder.Build(context);
        var reportPath = Path.Combine(reportDir, $"report_{sessionKey}.html");
        File.WriteAllText(reportPath, html, System.Text.Encoding.UTF8);
        return reportPath;
    }

    public bool TryGetReportPath(string sessionKey, out string reportPath)
    {
        reportPath = Path.Combine(reportDir, $"report_{sessionKey}.html");
        return File.Exists(reportPath);
    }

    /// <summary>
    /// True when uploads exist but the HTML is missing, or a session file is newer than the HTML.
    /// The ready callback can land before the last upload, and the cached report would stay empty.
    /// </summary>
    public bool NeedsRegenerate(string sessionKey)
    {
        var files = uploadIndex.GetSessionFiles(sessionKey);
        var reportPath = Path.Combine(reportDir, $"report_{sessionKey}.html");
        if (!File.Exists(reportPath))
        {
            return files.Count > 0;
        }

        var reportTime = File.GetLastWriteTimeUtc(reportPath);
        foreach (var file in files)
        {
            if (!string.IsNullOrEmpty(file.SavedPath)
                && File.Exists(file.SavedPath)
                && File.GetLastWriteTimeUtc(file.SavedPath) > reportTime)
            {
                return true;
            }
        }

        return false;
    }
}
