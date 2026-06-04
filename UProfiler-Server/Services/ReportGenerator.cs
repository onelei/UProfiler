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
}
