using System;
using System.Collections.Generic;
using System.Linq;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>Optional Lua memory provider hook for luaMemory_ upload (xLua / SLua / custom).</summary>
    public static class LuaMemoryProvider
    {
        public delegate LuaMemoryUploadData SnapshotDelegate(int frameIndex);

        public static SnapshotDelegate SnapshotHandler { get; set; }

        public static LuaMemoryUploadData TryBuildSnapshot(int frameIndex)
        {
            return SnapshotHandler?.Invoke(frameIndex);
        }
    }
}
