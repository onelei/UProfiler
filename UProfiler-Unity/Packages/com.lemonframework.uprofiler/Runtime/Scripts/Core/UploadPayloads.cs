using System;
using System.Collections.Generic;

namespace LemonFramework.UProfiler.Core
{
    [Serializable]
    public class ModuleTimeUploadData
    {
        public List<int> x = new List<int>();
        public Dictionary<string, List<double>> series = new Dictionary<string, List<double>>();
        public List<ModuleMetaRow> modules = new List<ModuleMetaRow>();
        public List<ModuleSummaryUploadRow> summary = new List<ModuleSummaryUploadRow>();
    }

    [Serializable]
    public class ModuleMetaRow
    {
        public string key = "";
        public string label = "";
        public string color = "";
        public double recommendMs;
    }

    [Serializable]
    public class ModuleSummaryUploadRow
    {
        public string key = "";
        public string label = "";
        public string color = "";
        public double averageMs;
        public double recommendMs;
        public bool overRecommend;
    }

    [Serializable]
    public class ThreadStackUploadData
    {
        public List<ThreadStackThreadUploadRow> threads = new List<ThreadStackThreadUploadRow>();
    }

    [Serializable]
    public class ThreadStackThreadUploadRow
    {
        public string name = "";
        public double avgCpuMs;
        public List<ModuleFuncStackFunctionRow> functions = new List<ModuleFuncStackFunctionRow>();
    }

    [Serializable]
    public class ModuleFuncStackUploadData
    {
        public string module = "";
        public string scope = "overview";
        public string stackMode = "module";
        public string order = "forward";
        public List<ModuleFuncStackMetricRow> metrics = new List<ModuleFuncStackMetricRow>();
        public List<ModuleFuncStackFunctionRow> functions = new List<ModuleFuncStackFunctionRow>();
        public List<ModuleFuncStackAiRow> aiDiagnosis = new List<ModuleFuncStackAiRow>();
    }

    [Serializable]
    public class ModuleFuncStackMetricRow
    {
        public string label = "";
        public double avgMs;
        public double peakMs;
        public int peakFrame;
        public string unit = "ms";
        public string statLabel = "均值";
    }

    [Serializable]
    public class ModuleFuncStackFunctionRow
    {
        public string name = "";
        public double avgMs;
        public double totalMs;
        public double selfMs;
        public double totalPct;
        public double selfPct;
        public int callCount;
        public double callsPerFrame;
        public int frameCount;
    }

    [Serializable]
    public class ModuleFuncStackAiRow
    {
        public string title = "";
        public string severity = "Low";
        public string suggestion = "";
    }

    [Serializable]
    public class BriefAiDiagnosisUploadData
    {
        public List<BriefAiMetricUploadRow> metrics = new List<BriefAiMetricUploadRow>();
    }

    [Serializable]
    public class BriefAiMetricUploadRow
    {
        public string name = "";
        public double value;
        public string unit = "";
        public string industryRank = "-";
        public int optimizeCount;
        public List<BriefAiDiagnosisEntryRow> diagnosis = new List<BriefAiDiagnosisEntryRow>();
    }

    [Serializable]
    public class BriefAiDiagnosisEntryRow
    {
        public string severity = "Low";
        public List<string> roles = new List<string>();
        public string title = "";
        public string value = "";
        public string suggestion = "";
    }

    [Serializable]
    public class HardwareInfoUploadData
    {
        public int targetFrameRate;
        public string networkType = "";
        public List<HardwareSampleRow> samples = new List<HardwareSampleRow>();
    }

    [Serializable]
    public class HardwareSampleRow
    {
        public int frameIndex;
        public double cpuFreqMHz;
        public double netSentKB;
        public double netRecvKB;
        public bool lowMemory;
    }

    [Serializable]
    public class GpuBandwidthUploadData
    {
        public List<GpuBandwidthSampleRow> samples = new List<GpuBandwidthSampleRow>();
    }

    [Serializable]
    public class GpuBandwidthSampleRow
    {
        public int frameIndex;
        public long readBytes;
        public long writeBytes;
        public long totalBytes;
    }

    [Serializable]
    public class ResourceManagementUploadData
    {
        public double resourcesLoadPer1k;
        public double abLoadPer1k;
        public double instantiatePer1k;
        public double activatePer1k;
        public List<ResourceManagementTopRow> abLoadTop = new List<ResourceManagementTopRow>();
        public List<ResourceManagementTopRow> resourceLoadTop = new List<ResourceManagementTopRow>();
        public List<ResourceManagementTopRow> instantiateTop = new List<ResourceManagementTopRow>();
        public List<ResourceManagementTopRow> unloadTop = new List<ResourceManagementTopRow>();
        public List<ResourceManagementEventRow> assetBundle = new List<ResourceManagementEventRow>();
        public List<ResourceManagementEventRow> resource = new List<ResourceManagementEventRow>();
        public List<ResourceManagementEventRow> instantiate = new List<ResourceManagementEventRow>();
    }

    [Serializable]
    public class ResourceManagementEventRow
    {
        public int frame;
        public string action = "";
        public string name = "";
        public string path = "";
        public string scene = "";
        public double durationMs;
    }

    [Serializable]
    public class ResourceManagementTopRow
    {
        public string name = "";
        public string path = "";
        public string loadMode = "";
        public int count;
    }

    [Serializable]
    public class CustomDashboardUploadData
    {
        public List<CustomDashboardPanelRow> panels = new List<CustomDashboardPanelRow>();
    }

    [Serializable]
    public class CustomDashboardPanelRow
    {
        public string name = "";
        public List<CustomDashboardMetricRow> metrics = new List<CustomDashboardMetricRow>();
    }

    [Serializable]
    public class CustomDashboardMetricRow
    {
        public string label = "";
        public string unit = "";
        public List<int> frames = new List<int>();
        public List<double> values = new List<double>();
    }

    [Serializable]
    public class CustomFuncsUploadData
    {
        public List<CustomFuncGroupRow> groups = new List<CustomFuncGroupRow>();
    }

    [Serializable]
    public class CustomFuncGroupRow
    {
        public string groupName = "";
        public List<ModuleFuncStackFunctionRow> functions = new List<ModuleFuncStackFunctionRow>();
    }

    [Serializable]
    public class CustomVarsUploadData
    {
        public List<string> varNames = new List<string>();
        public List<CustomVarSampleRow> samples = new List<CustomVarSampleRow>();
    }

    [Serializable]
    public class CustomVarSampleRow
    {
        public int frameIndex;
        public string varName = "";
        public string value = "";
    }

    [Serializable]
    public class CustomCodeUploadData
    {
        public List<CustomCodeSegmentRow> segments = new List<CustomCodeSegmentRow>();
    }

    [Serializable]
    public class CustomCodeSegmentRow
    {
        public string name = "";
        public int startFrame;
        public int endFrame;
        public double totalMs;
    }

    [Serializable]
    public class LuaMemoryUploadData
    {
        public List<string> subTabs = new List<string>();
        public List<string> heapMetrics = new List<string>();
        public List<LuaMemoryCurveRow> curves = new List<LuaMemoryCurveRow>();
        public List<LuaHeapAllocationRow> allocations = new List<LuaHeapAllocationRow>();
        public List<LuaMonoRefRow> monoRefs = new List<LuaMonoRefRow>();
        public List<ModuleFuncStackAiRow> aiDiagnosis = new List<ModuleFuncStackAiRow>();
    }

    [Serializable]
    public class LuaMemoryCurveRow
    {
        public string label = "";
        public string unit = "KB";
        public List<double> values = new List<double>();
        public List<int> frames = new List<int>();
    }

    [Serializable]
    public class LuaHeapAllocationRow
    {
        public string type = "";
        public long sizeBytes;
        public int count;
        public string functionName = "";
        public double avgAlloc;
    }

    [Serializable]
    public class LuaMonoRefRow
    {
        public string objectName = "";
        public int refCount;
        public int destroyedCount;
    }
}
