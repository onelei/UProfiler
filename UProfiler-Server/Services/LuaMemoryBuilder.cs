using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class LuaMemoryBuilder
{
    static readonly string[] DefaultHeapMetrics =
    [
        "LUA堆内存",
        "Table数量",
        "Function数量",
        "Userdata数量"
    ];

    static readonly string[] DefaultSubTabs =
    [
        "总体堆内存",
        "堆内存具体分配",
        "Mono对象引用"
    ];

    public static LuaMemoryDto? Normalize(LuaMemoryDto? raw)
    {
        if (raw == null || raw.Curves.Count == 0)
        {
            return raw;
        }

        var curves = raw.Curves
            .Select(curve => new LuaMemoryCurveDto
            {
                Label = curve.Label,
                Unit = string.IsNullOrWhiteSpace(curve.Unit) ? ResolveUnit(curve.Label) : curve.Unit,
                Frames = curve.Frames,
                Values = curve.Values
            })
            .Where(curve => curve.Frames.Count > 0 && curve.Values.Count > 0)
            .ToList();

        if (curves.Count == 0)
        {
            return new LuaMemoryDto
            {
                SubTabs = raw.SubTabs,
                HeapMetrics = raw.HeapMetrics,
                Curves = curves,
                Allocations = raw.Allocations,
                MonoRefs = raw.MonoRefs,
                AiDiagnosis = raw.AiDiagnosis
            };
        }

        return new LuaMemoryDto
        {
            SubTabs = raw.SubTabs.Count > 0 ? raw.SubTabs : DefaultSubTabs.ToList(),
            HeapMetrics = raw.HeapMetrics.Count > 0 ? raw.HeapMetrics : DefaultHeapMetrics.ToList(),
            Curves = curves,
            Allocations = raw.Allocations,
            MonoRefs = raw.MonoRefs,
            AiDiagnosis = raw.AiDiagnosis
        };
    }

    static string ResolveUnit(string label) =>
        label switch
        {
            "LUA堆内存" => "KB",
            "Table数量" or "Function数量" or "Userdata数量" => "个",
            _ => "KB"
        };
}
