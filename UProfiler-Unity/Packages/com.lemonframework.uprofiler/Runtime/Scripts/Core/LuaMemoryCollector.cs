using System;
using System.Collections.Generic;
using System.Linq;

namespace LemonFramework.UProfiler.Core
{
    public static class LuaMemoryCollector
    {
        static readonly (string label, string unit)[] MetricDefs =
        {
            ("LUA堆内存", "KB"),
            ("Table数量", "个"),
            ("Function数量", "个"),
            ("Userdata数量", "个")
        };

        static readonly Dictionary<string, LuaMemoryCurveRow> Curves =
            new Dictionary<string, LuaMemoryCurveRow>(StringComparer.Ordinal);

        static readonly List<LuaHeapAllocationRow> Allocations = new List<LuaHeapAllocationRow>();
        static readonly List<LuaMonoRefRow> MonoRefs = new List<LuaMonoRefRow>();
        static readonly List<ModuleFuncStackAiRow> AiDiagnosis = new List<ModuleFuncStackAiRow>();

        public static void Clear()
        {
            Curves.Clear();
            Allocations.Clear();
            MonoRefs.Clear();
            AiDiagnosis.Clear();
        }

        public static void RecordMetrics(
            int frameIndex,
            double luaHeapKb,
            int tableCount,
            int functionCount,
            int userdataCount)
        {
            AddPoint("LUA堆内存", frameIndex, luaHeapKb);
            AddPoint("Table数量", frameIndex, tableCount);
            AddPoint("Function数量", frameIndex, functionCount);
            AddPoint("Userdata数量", frameIndex, userdataCount);
        }

        public static void MergeSnapshot(LuaMemoryUploadData snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            if (snapshot.curves != null)
            {
                foreach (var curve in snapshot.curves)
                {
                    if (curve?.frames == null || curve.values == null)
                    {
                        continue;
                    }

                    for (var i = 0; i < curve.frames.Count && i < curve.values.Count; i++)
                    {
                        AddPoint(curve.label, curve.frames[i], curve.values[i], curve.unit);
                    }
                }
            }

            if (snapshot.allocations != null)
            {
                Allocations.AddRange(snapshot.allocations);
            }

            if (snapshot.monoRefs != null)
            {
                MonoRefs.AddRange(snapshot.monoRefs);
            }

            if (snapshot.aiDiagnosis != null)
            {
                AiDiagnosis.AddRange(snapshot.aiDiagnosis);
            }
        }

        public static LuaMemoryUploadData BuildPayload()
        {
            if (!Curves.Values.Any(curve => curve.frames.Count > 0))
            {
                return null;
            }

            return new LuaMemoryUploadData
            {
                subTabs = new List<string> { "总体堆内存", "堆内存具体分配", "Mono对象引用" },
                heapMetrics = MetricDefs.Select(item => item.label).ToList(),
                curves = Curves.Values.ToList(),
                allocations = Allocations.ToList(),
                monoRefs = MonoRefs.ToList(),
                aiDiagnosis = AiDiagnosis.ToList()
            };
        }

        static void AddPoint(string label, int frameIndex, double value, string unit = null)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            if (!Curves.TryGetValue(label, out var curve))
            {
                curve = new LuaMemoryCurveRow
                {
                    label = label,
                    unit = string.IsNullOrEmpty(unit) ? ResolveUnit(label) : unit
                };
                Curves[label] = curve;
            }

            curve.frames.Add(frameIndex);
            curve.values.Add(Math.Round(value, 2));
        }

        static string ResolveUnit(string label)
        {
            foreach (var def in MetricDefs)
            {
                if (def.label == label)
                {
                    return def.unit;
                }
            }

            return "KB";
        }
    }
}
