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

    [JsonPropertyName("MonitorInfoList")]
    public List<UProfilerInfoDto> MonitorInfoList { get; set; } = new();

    public List<UProfilerInfoDto> GetAll() =>
        UProfilerInfoList.Count > 0 ? UProfilerInfoList : MonitorInfoList;
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

public sealed class SceneSegmentDto
{
    public string SceneName { get; set; } = "";
    public int StartFrame { get; set; }
    public int EndFrame { get; set; }
    public string? Note { get; set; }
}

public sealed class SceneInfoDto
{
    public List<SceneSegmentDto> Segments { get; set; } = new();
}

public sealed class FrameTimePoint
{
    public int FrameIndex { get; init; }
    public double FrameMs { get; init; }
}

public sealed class SceneTableRow
{
    public string SceneName { get; init; } = "";
    public int StartFrame { get; init; }
    public int EndFrame { get; init; }
    public int FrameCount { get; init; }
    public double AvgFrameMs { get; init; }
    public double AvgFps { get; init; }
    public float PeakPssMb { get; init; }
    public double PeakMonoMb { get; init; }
    public double PeakCpuMs { get; init; }
    public long PeakTriangles { get; init; }
    public long PeakDrawCall { get; init; }
    public string Note { get; init; } = "";
}

public sealed class SceneOverviewBarRow
{
    public string SceneName { get; init; } = "";
    public Dictionary<string, double> ModuleMs { get; init; } = new();
}

public sealed class SceneManagementPayload
{
    public bool HasSceneInfo { get; init; }
    public List<FrameTimePoint> FrameTimes { get; init; } = new();
    public List<SceneTableRow> Scenes { get; init; } = new();
    public List<SceneOverviewBarRow> OverviewBars { get; init; } = new();
    public List<string> OverviewModules { get; init; } = new();
}

public sealed class RecordResInfoDto
{
    public int FrameIndex { get; set; }
    public long TextureSize { get; set; }
    public int TextureCount { get; set; }
    public long MeshSize { get; set; }
    public int MeshCount { get; set; }
    public long MaterialSize { get; set; }
    public int MaterialCount { get; set; }
    public long ShaderSize { get; set; }
    public int ShaderCount { get; set; }
    public long AnimationClipSize { get; set; }
    public int AnimationClipCount { get; set; }
    public long AudioClipSize { get; set; }
    public int AudioClipCount { get; set; }
    public long FontSize { get; set; }
    public int FontCount { get; set; }
    public long TextAssetSize { get; set; }
    public int TextAssetCount { get; set; }
    public long ScriptableObjectSize { get; set; }
    public int ScriptableObjectCount { get; set; }
    public long TotalSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class RecordResInfosDto
{
    [JsonPropertyName("recordResInfosList")]
    public List<RecordResInfoDto> RecordResInfosList { get; set; } = new();
}

public sealed class BriefKpiCard
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public string Value { get; init; } = "-";
    public string Unit { get; init; } = "";
    public int TaskCount { get; init; }
}

public sealed class BriefMetricRow
{
    public string Label { get; init; } = "";
    public string Value { get; init; } = "-";
    public string Unit { get; init; } = "";
    public int TaskCount { get; init; }
}

public sealed class PerformanceBriefPayload
{
    public int FrameCount { get; init; }
    public string SummaryText { get; init; } = "";
    public int OptimizableCount { get; init; }
    public int TotalMetricCount { get; init; }
    public List<BriefKpiCard> Kpis { get; init; } = new();
    public List<BriefMetricRow> Metrics { get; init; } = new();
}

public sealed class JankFrameRow
{
    public int FrameIndex { get; init; }
    public int Fps { get; init; }
    public double FrameMs { get; init; }
    public string JankType { get; init; } = "Jank";
}

public sealed class JankFunctionRow
{
    public string Name { get; init; } = "";
    public double AverageMs { get; init; }
    public int Calls { get; init; }
    public double TotalSeconds { get; init; }
}

public sealed class JankHotFunctionRow
{
    public string Name { get; init; } = "";
    public int KeyJankCount { get; init; }
    public double TotalRatio { get; init; }
    public double SelfRatio { get; init; }
    public double TotalMs { get; init; }
    public double SelfMs { get; init; }
    public int SpreadJankCount { get; init; }
}

public sealed class JankAnalysisPayload
{
    public double JankPerMinute { get; init; }
    public int JankCount { get; init; }
    public int BigJankCount { get; init; }
    public int SevereJankCount { get; init; }
    public int LoadingJankCount { get; init; }
    public int OtherJankCount { get; init; }
    public List<JankFrameRow> Frames { get; init; } = new();
    public List<JankFunctionRow> HotFunctions { get; init; } = new();
    public List<JankHotFunctionRow> JankHotFunctions { get; init; } = new();
}

public sealed class ResourceSummaryRow
{
    public string Type { get; init; } = "";
    public string Label { get; init; } = "";
    public long AvgSizeBytes { get; init; }
    public int AvgCount { get; init; }
    public long PeakSizeBytes { get; init; }
    public string RecommendText { get; init; } = "-";
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
    public RecordResInfosDto? ResourceMemory { get; init; }
    public SceneInfoDto? SceneInfo { get; init; }
    public SceneManagementPayload SceneManagement { get; init; } = new();
    public List<FuncAnalysisInfoDto> FuncAnalysis { get; init; } = new();
    public List<string> LogLines { get; init; } = new();
    public IReadOnlyList<SessionUpload> Files { get; init; } = Array.Empty<SessionUpload>();
    public CaptureFrameManifest CaptureFrames { get; init; } = new();
    public ModuleTimePayload ModuleTime { get; init; } = new();
    public Dictionary<string, ModuleDetailPayload> ModuleDetails { get; init; } = new();
    public PerformanceBriefPayload Brief { get; init; } = new();
    public JankAnalysisPayload Jank { get; init; } = new();
    public List<ResourceSummaryRow> ResourceSummary { get; init; } = new();

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
