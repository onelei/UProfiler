using System.Text.Json.Serialization;

namespace UProfiler.Server.Models;

public sealed class TestInfoDto
{
    public string ProductName { get; set; } = "";
    public string PackageName { get; set; } = "";
    public string Platform { get; set; } = "";
    public string Version { get; set; } = "";
    public string TestTime { get; set; } = "";
    public int IntervalFrame { get; set; }
}

public sealed class DeviceInfoDto
{
    public string UnityVersion { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public string DeviceModel { get; set; } = "";
    public string DeviceName { get; set; } = "";
    public string DeviceUniqueIdentifier { get; set; } = "";
    public int SystemMemorySize { get; set; }
    public int GraphicsMemorySize { get; set; }
    public string ProcessorType { get; set; } = "";
    public int ProcessorFrequency { get; set; }
    public int ProcessorCount { get; set; }
    public string GraphicsDeviceName { get; set; } = "";
    public string GraphicsDeviceVendor { get; set; } = "";
    public string GraphicsDeviceVersion { get; set; } = "";
    public bool SupportsShadows { get; set; }
    public float BatteryLevel { get; set; }
    public int ScreenWidth { get; set; }
    public int ScreenHeight { get; set; }
}

public sealed class UProfilerFrameInfoDto
{
    public int FrameIndex { get; set; }
    public int Frame { get; set; }
}

public sealed class FrameRatesDto
{
    [JsonPropertyName("FrameRateList")]
    public List<UProfilerFrameInfoDto> FrameRateList { get; set; } = new();
}

public sealed class UProfilerInfoDto
{
    public int FrameIndex { get; set; }
    public float BatteryLevel { get; set; }
    public int MemorySize { get; set; }
    public int Frame { get; set; }
    public long MonoHeapSize { get; set; }
    public long MonoUsedSize { get; set; }
    public long AllocatedMemoryForGraphicsDriver { get; set; }
    public long TotalAllocatedMemory { get; set; }
    public long UnityTotalReservedMemory { get; set; }
    public long TotalUnusedReservedMemory { get; set; }
}

public sealed class UProfilerInfosDto
{
    [JsonPropertyName("UProfilerInfoList")]
    public List<UProfilerInfoDto> UProfilerInfoList { get; set; } = new();
}

public sealed class RenderInfoDto
{
    public int FrameIndex { get; set; }
    public long SetPassCall { get; set; }
    public long DrawCall { get; set; }
    public long Vertices { get; set; }
    public long Triangles { get; set; }
}

public sealed class RenderInfosDto
{
    [JsonPropertyName("RenderInfoList")]
    public List<RenderInfoDto> RenderInfoList { get; set; } = new();
}

public sealed class FuncAnalysisInfoDto
{
    public string Name { get; set; } = "";
    public double Memory { get; set; }
    public double AverageMemory { get; set; }
    public float UseTime { get; set; }
    public float AverageTime { get; set; }
    public int Calls { get; set; }
}

public sealed class MemoryUseDataDto
{
    public int FrameIndex { get; set; }
    public float PssMemorySize { get; set; }
}

public sealed class MemoryUseDatasDto
{
    [JsonPropertyName("MemoryUsedList")]
    public List<MemoryUseDataDto> MemoryUsedList { get; set; } = new();
}

public sealed class DevicePowerConsumeInfoDto
{
    public int FrameIndex { get; set; }
    public int Temperature { get; set; }
    public float BatteryPower { get; set; }
    public int CpuTemperate { get; set; }
    public int BatteryCapacity { get; set; }
}

public sealed class DevicePowerConsumeInfosDto
{
    [JsonPropertyName("devicePowerConsumeInfos")]
    public List<DevicePowerConsumeInfoDto> DevicePowerConsumeInfos { get; set; } = new();
}

public sealed class SessionUpload
{
    public required string OriginalName { get; init; }
    public required string SavedPath { get; init; }
    public required string Prefix { get; init; }
}

public sealed class DiagnosisItem
{
    public required string Id { get; init; }
    public required string Category { get; init; }
    public required string Title { get; init; }
    public required string Severity { get; init; }
    public required string ValueText { get; init; }
    public string IndustryText { get; init; } = "-";
    public string RecommendText { get; init; } = "-";
    public string Summary { get; init; } = "";
    public List<string> Suggestions { get; init; } = new();
}

public sealed class CaptureFrameManifest
{
    public string SessionKey { get; init; } = "";
    public SortedDictionary<int, string> FrameImages { get; init; } = new();
    public string? DeviceModel { get; init; }
}

public sealed class ModuleMeta
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string Color { get; init; } = "";
    public double RecommendMs { get; init; }
}

public sealed class ModuleSummaryRow
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string Color { get; init; } = "";
    public double AverageMs { get; init; }
    public double RecommendMs { get; init; }
    public bool OverRecommend { get; init; }
}

public sealed class ModuleTimePayload
{
    public List<ModuleMeta> Modules { get; init; } = new();
    public List<int> X { get; init; } = new();
    public Dictionary<string, List<double>> Series { get; init; } = new();
    public List<ModuleSummaryRow> Summary { get; init; } = new();
}

public sealed class ModuleDetailPieSlice
{
    public string Name { get; init; } = "";
    public double Value { get; init; }
    public string Color { get; init; } = "";
}

public sealed class ModuleDetailMetricRow
{
    public string Name { get; init; } = "";
    public double AverageMs { get; init; }
    public double Ratio { get; init; }
    public string Unit { get; init; } = "ms";
    public string? LinkTarget { get; init; }
}

public sealed class ModuleDetailSeries
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string Color { get; init; } = "";
    public List<double> Data { get; init; } = new();
    public int YAxisIndex { get; init; }
    public string Unit { get; init; } = "ms";
}

public sealed class ModuleDetailPayload
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string Title { get; init; } = "";
    public string DetailTitle { get; init; } = "";
    public string PieTitle { get; init; } = "";
    public string ChartTitle { get; init; } = "";
    public string Color { get; init; } = "";
    public bool HasDrillDown { get; init; }
    public string? EmptyHint { get; init; }
    public List<ModuleDetailPieSlice> PieSlices { get; init; } = new();
    public List<ModuleDetailMetricRow> Metrics { get; init; } = new();
    public List<int> X { get; init; } = new();
    public List<ModuleDetailSeries> Series { get; init; } = new();
    public bool DualAxis { get; init; }
}

public sealed record ReportDataContext
{
    public required string SessionKey { get; init; }
    public string? PackageName { get; init; }
    public TestInfoDto? TestInfo { get; init; }
    public DeviceInfoDto? DeviceInfo { get; init; }
    public FrameRatesDto? FrameRates { get; init; }
    public UProfilerInfosDto? UProfilerInfos { get; init; }
    public RenderInfosDto? RenderInfos { get; init; }
    public MemoryUseDatasDto? MemoryUseDatas { get; init; }
    public DevicePowerConsumeInfosDto? PowerInfos { get; init; }
    public List<FuncAnalysisInfoDto> FuncAnalysis { get; init; } = new();
    public List<string> LogLines { get; init; } = new();
    public IReadOnlyList<SessionUpload> Files { get; init; } = Array.Empty<SessionUpload>();
    public CaptureFrameManifest CaptureFrames { get; init; } = new();
    public ModuleTimePayload ModuleTime { get; init; } = new();
    public Dictionary<string, ModuleDetailPayload> ModuleDetails { get; init; } = new();

    public double AvgFps { get; init; }
    public int MinFps { get; init; }
    public int MaxFps { get; init; }
    public long PeakMonoUsed { get; init; }
    public long PeakTotalAllocated { get; init; }
    public long PeakDrawCall { get; init; }
    public long PeakTriangles { get; init; }
    public float PeakPssMb { get; init; }
    public float PeakBatteryPower { get; init; }
    public int PeakCpuTemp { get; init; }
    public List<DiagnosisItem> DiagnosisItems { get; init; } = new();
}
