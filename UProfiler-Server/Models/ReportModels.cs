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

public sealed class HardwareSampleDto
{
    public int FrameIndex { get; set; }
    public double CpuFreqMHz { get; set; }
    public double NetSentKB { get; set; }
    public double NetRecvKB { get; set; }
    public bool LowMemory { get; set; }
}

public sealed class HardwareInfoDto
{
    public int TargetFrameRate { get; set; }
    public string NetworkType { get; set; } = "";
    public List<HardwareSampleDto> Samples { get; set; } = new();
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
    public string IndustryRank { get; init; } = "-";
    public string ModuleKey { get; init; } = "";
    public List<BriefDiagnosisEntry> Diagnosis { get; init; } = new();
}

public sealed class BriefDiagnosisEntry
{
    public string Severity { get; init; } = "Low";
    public List<string> Roles { get; init; } = new();
    public string Title { get; init; } = "";
    public string Value { get; init; } = "";
    public string Suggestion { get; init; } = "";
}

public sealed class ThreadStackFunctionRow
{
    public string Name { get; init; } = "";
    public double AvgMs { get; init; }
    public double TotalMs { get; init; }
    public double SelfMs { get; init; }
    public double TotalPct { get; init; }
    public double SelfPct { get; init; }
    public int CallCount { get; init; }
    public double CallsPerFrame { get; init; }
    public int FrameCount { get; init; }
}

public sealed class ThreadStackThreadRow
{
    public string Name { get; init; } = "";
    public double AvgCpuMs { get; init; }
    public List<ThreadStackFunctionRow> Functions { get; init; } = new();
}

public sealed class ThreadStackPayload
{
    public List<ThreadStackThreadRow> Threads { get; init; } = new();
}

public sealed class ModuleFuncStackFunctionRow
{
    public string Name { get; init; } = "";
    public double AvgMs { get; init; }
    public double TotalMs { get; init; }
    public double SelfMs { get; init; }
    public double TotalPct { get; init; }
    public double SelfPct { get; init; }
    public int CallCount { get; init; }
    public double CallsPerFrame { get; init; }
    public int FrameCount { get; init; }
}

public sealed class ModuleFuncStackMetricRow
{
    public string Label { get; init; } = "";
    public double AvgMs { get; init; }
    public double PeakMs { get; init; }
    public int PeakFrame { get; init; }

    /// <summary>指标单位，默认 ms；支持 个/次/% 等非耗时指标（对应 UWA GL Batches、SyncTransform 调用次数等）。</summary>
    public string Unit { get; init; } = "ms";

    /// <summary>统计口径文案：均值 / 峰值 / 显著帧均值 / 调用频率。</summary>
    public string StatLabel { get; init; } = "均值";
}

public sealed class ModuleFuncStackAiEntry
{
    public string Title { get; init; } = "";
    public string Severity { get; init; } = "Low";
    public string Suggestion { get; init; } = "";
}

public sealed class ModuleFuncStackDto
{
    public string Module { get; init; } = "";
    public string Scope { get; init; } = "overview";
    public string StackMode { get; init; } = "module";
    public string Order { get; init; } = "forward";
    public List<ModuleFuncStackMetricRow> Metrics { get; init; } = new();
    public List<ModuleFuncStackFunctionRow> Functions { get; init; } = new();
    public List<ModuleFuncStackAiEntry> AiDiagnosis { get; init; } = new();
}

public sealed class BriefAiDiagnosisDto
{
    public List<BriefAiMetricDto> Metrics { get; init; } = new();
}

public sealed class BriefAiMetricDto
{
    public string Name { get; init; } = "";
    public double Value { get; init; }
    public string Unit { get; init; } = "";
    public string IndustryRank { get; init; } = "-";
    public int OptimizeCount { get; init; }
    public List<BriefDiagnosisEntry> Diagnosis { get; init; } = new();
}

public sealed class GpuBandwidthSampleDto
{
    public int FrameIndex { get; set; }
    public long ReadBytes { get; set; }
    public long WriteBytes { get; set; }
    public long TotalBytes { get; set; }
}

public sealed class GpuBandwidthDto
{
    public List<GpuBandwidthSampleDto> Samples { get; set; } = new();
}

public sealed class LuaMemoryCurveDto
{
    public string Label { get; init; } = "";
    public string Unit { get; init; } = "KB";
    public List<double> Values { get; init; } = new();
    public List<int> Frames { get; init; } = new();
}

public sealed class LuaHeapAllocationDto
{
    public string Type { get; init; } = "";
    public long SizeBytes { get; init; }
    public int Count { get; init; }
    public string FunctionName { get; init; } = "";
    public double AvgAlloc { get; init; }
}

public sealed class LuaMonoRefDto
{
    public string ObjectName { get; init; } = "";
    public int RefCount { get; init; }
    public int DestroyedCount { get; init; }
}

public sealed class LuaMemoryDto
{
    public List<string> SubTabs { get; init; } = new();
    public List<string> HeapMetrics { get; init; } = new();
    public List<LuaMemoryCurveDto> Curves { get; init; } = new();
    public List<LuaHeapAllocationDto> Allocations { get; init; } = new();
    public List<LuaMonoRefDto> MonoRefs { get; init; } = new();
    public List<ModuleFuncStackAiEntry> AiDiagnosis { get; init; } = new();
}

public sealed class ResourceManagementEventDto
{
    public int Frame { get; init; }
    public string Action { get; init; } = "";
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string Scene { get; init; } = "";
    public double DurationMs { get; init; }
}

public sealed class ResourceManagementTopDto
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string LoadMode { get; init; } = "";
    public int Count { get; init; }
}

public sealed class ResourceManagementDto
{
    public double ResourcesLoadPer1k { get; init; }
    public double AbLoadPer1k { get; init; }
    public double InstantiatePer1k { get; init; }
    public double ActivatePer1k { get; init; }
    public List<ResourceManagementTopDto> AbLoadTop { get; init; } = new();
    public List<ResourceManagementTopDto> ResourceLoadTop { get; init; } = new();
    public List<ResourceManagementTopDto> InstantiateTop { get; init; } = new();
    public List<ResourceManagementTopDto> UnloadTop { get; init; } = new();
    public List<ResourceManagementEventDto> AssetBundle { get; init; } = new();
    public List<ResourceManagementEventDto> Resource { get; init; } = new();
    public List<ResourceManagementEventDto> Instantiate { get; init; } = new();
}

public sealed class CustomDashboardMetricDto
{
    public string Label { get; init; } = "";
    public string Unit { get; init; } = "";
    public List<int> Frames { get; init; } = new();
    public List<double> Values { get; init; } = new();
}

public sealed class CustomDashboardPanelDto
{
    public string Name { get; init; } = "";
    public List<CustomDashboardMetricDto> Metrics { get; init; } = new();
}

public sealed class CustomDashboardDto
{
    public List<CustomDashboardPanelDto> Panels { get; init; } = new();
}

public sealed class CustomFuncGroupDto
{
    public string GroupName { get; init; } = "";
    public List<ModuleFuncStackFunctionRow> Functions { get; init; } = new();
}

public sealed class CustomFuncsDto
{
    public List<CustomFuncGroupDto> Groups { get; init; } = new();
}

public sealed class CustomVarSampleDto
{
    public int FrameIndex { get; init; }
    public string VarName { get; init; } = "";
    public string Value { get; init; } = "";
}

public sealed class CustomVarsDto
{
    public List<string> VarNames { get; init; } = new();
    public List<CustomVarSampleDto> Samples { get; init; } = new();
}

public sealed class CustomCodeSegmentDto
{
    public string Name { get; init; } = "";
    public int StartFrame { get; init; }
    public int EndFrame { get; init; }
    public double TotalMs { get; init; }
}

public sealed class CustomCodeDto
{
    public List<CustomCodeSegmentDto> Segments { get; init; } = new();
}

public sealed class JankFuncCategoryPayload
{
    public string Key { get; init; } = "";
    public string Label { get; init; } = "";
    public List<JankHotFunctionRow> Functions { get; init; } = new();
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
    public int GcJankCount { get; init; }
    public int UnloadJankCount { get; init; }
    public int AnimationJankCount { get; init; }
    public int PhysicsJankCount { get; init; }
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
    public ThreadStackPayload ThreadStack { get; init; } = new();
    public BriefAiDiagnosisDto? BriefAiDiagnosis { get; init; }
    public HardwareInfoDto? HardwareInfo { get; init; }
    public GpuBandwidthDto? GpuBandwidth { get; init; }
    public LuaMemoryDto? LuaMemory { get; init; }
    public ResourceManagementDto? ResourceManagement { get; init; }
    public Dictionary<string, ModuleFuncStackDto> ModuleFuncStacks { get; init; } = new();
    public CustomDashboardDto? CustomDashboard { get; init; }
    public CustomFuncsDto? CustomFuncs { get; init; }
    public CustomVarsDto? CustomVars { get; init; }
    public CustomCodeDto? CustomCode { get; init; }
    public List<JankFuncCategoryPayload> JankFuncCategories { get; init; } = new();

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
