using System;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>Optional Lua memory provider hook for luaMemory_ upload (xLua / SLua / custom).</summary>
    public static class LuaMemoryProvider
    {
        public delegate LuaMemoryUploadData SnapshotDelegate(int frameIndex);

        public static SnapshotDelegate SnapshotHandler { get; set; }

        public static void RegisterLuaEnv(object luaEnv)
        {
            LuaMemoryAutoProbe.RegisterLuaEnv(luaEnv);
        }

        public static void RecordMetrics(
            int frameIndex,
            double luaHeapKb,
            int tableCount,
            int functionCount,
            int userdataCount)
        {
            LuaMemoryCollector.RecordMetrics(frameIndex, luaHeapKb, tableCount, functionCount, userdataCount);
        }

        public static void Collect(int frameIndex)
        {
            var snapshot = SnapshotHandler?.Invoke(frameIndex);
            if (snapshot != null)
            {
                LuaMemoryCollector.MergeSnapshot(snapshot);
                return;
            }

            if (LuaMemoryAutoProbe.TrySample(
                    frameIndex,
                    out var heapKb,
                    out var tableCount,
                    out var functionCount,
                    out var userdataCount))
            {
                LuaMemoryCollector.RecordMetrics(frameIndex, heapKb, tableCount, functionCount, userdataCount);
            }
        }
    }
}
