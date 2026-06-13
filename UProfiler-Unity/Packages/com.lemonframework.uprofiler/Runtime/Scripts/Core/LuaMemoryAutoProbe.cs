using System;
using System.Reflection;
using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>Best-effort Lua memory probe via reflection (xLua / custom LuaEnv).</summary>
    public static class LuaMemoryAutoProbe
    {
        static object _luaEnv;
        static PropertyInfo _memoryProperty;
        static MethodInfo _fullGcMethod;
        static bool _probeFailed;

        public static void RegisterLuaEnv(object luaEnv)
        {
            if (luaEnv == null)
            {
                return;
            }

            _luaEnv = luaEnv;
            _probeFailed = false;
            CacheMembers(luaEnv.GetType());
        }

        public static bool TrySample(int frameIndex, out double luaHeapKb, out int tableCount, out int functionCount, out int userdataCount)
        {
            luaHeapKb = 0;
            tableCount = 0;
            functionCount = 0;
            userdataCount = 0;

            if (_probeFailed)
            {
                return false;
            }

            if (_luaEnv == null && !TryResolveDefaultLuaEnv())
            {
                return false;
            }

            if (_memoryProperty == null)
            {
                return false;
            }

            try
            {
                var memoryValue = _memoryProperty.GetValue(_luaEnv);
                if (memoryValue == null)
                {
                    return false;
                }

                luaHeapKb = Convert.ToDouble(memoryValue);
                return luaHeapKb >= 0;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UProfiler] Lua memory probe failed: {ex.Message}");
                _probeFailed = true;
                return false;
            }
        }

        static bool TryResolveDefaultLuaEnv()
        {
            var luaEnvType = Type.GetType("XLua.LuaEnv, XLua")
                ?? Type.GetType("XLua.LuaEnv, Assembly-CSharp")
                ?? Type.GetType("XLua.LuaEnv");

            if (luaEnvType == null)
            {
                _probeFailed = true;
                return false;
            }

            object instance = null;
            var instanceProp = luaEnvType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
            if (instanceProp != null)
            {
                instance = instanceProp.GetValue(null);
            }

            if (instance == null)
            {
                var mainField = luaEnvType.GetField("mainState", BindingFlags.NonPublic | BindingFlags.Static);
                if (mainField != null)
                {
                    instance = mainField.GetValue(null);
                }
            }

            if (instance == null)
            {
                try
                {
                    instance = Activator.CreateInstance(luaEnvType);
                }
                catch
                {
                    _probeFailed = true;
                    return false;
                }
            }

            _luaEnv = instance;
            CacheMembers(luaEnvType);
            return _luaEnv != null;
        }

        static void CacheMembers(Type luaEnvType)
        {
            _memoryProperty = luaEnvType.GetProperty("Memroy", BindingFlags.Public | BindingFlags.Instance)
                ?? luaEnvType.GetProperty("Memory", BindingFlags.Public | BindingFlags.Instance);
            _fullGcMethod = luaEnvType.GetMethod("FullGc", BindingFlags.Public | BindingFlags.Instance);
            _probeFailed = _memoryProperty == null;
        }
    }
}
