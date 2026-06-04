namespace UProfiler.Server.Models;

public sealed class SessionSummary
{
    public required string SessionKey { get; init; }
    public string ProductName { get; init; } = "";
    public string PackageName { get; init; } = "";
    public string Platform { get; init; } = "";
    public string Version { get; init; } = "";
    public string TestTime { get; init; } = "";
    public string DeviceModel { get; init; } = "";
    public double AvgFps { get; init; }
    public int MinFps { get; init; }
    public int MaxFps { get; init; }
    public long PeakDrawCall { get; init; }
    public int DiagnosisHigh { get; init; }
    public int DiagnosisMedium { get; init; }
    public DateTime? UploadedAt { get; init; }
    public string Grade { get; init; } = "B";
    public double Score { get; init; }
}

public sealed class ProjectSummary
{
    public required string PackageName { get; init; }
    public string ProductName { get; init; } = "";
    public string Platform { get; init; } = "";
    public int ReportCount { get; init; }
    public SessionSummary? LatestSession { get; init; }
    public List<SessionSummary> Sessions { get; init; } = new();
}

public sealed class PortalCatalog
{
    public List<ProjectSummary> Projects { get; init; } = new();
    public List<SessionSummary> RecentSessions { get; init; } = new();
}
