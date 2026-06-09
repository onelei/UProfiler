using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;

namespace LemonFramework.UProfiler.Core
{
    public static class HookUtil
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        static readonly Thread MainThread = Thread.CurrentThread;
        static readonly Dictionary<string, FuncData> FunctionDatas = new Dictionary<string, FuncData>();

        public static void Begin(string methodName)
        {
            if (!UProfilerSettings.IsFunctionHookEnabled)
                return;

            // Only sample on the Unity main thread.
            if (Thread.CurrentThread == MainThread)
            {
                long tmpMemory = Profiler.GetTotalAllocatedMemoryLong();
                float tmpTime = Time.realtimeSinceStartup;
                if (FunctionDatas.ContainsKey(methodName))
                {
                    var tmp = FunctionDatas[methodName];
                    tmp.BeginMemory = tmpMemory;
                    tmp.BeginTime = tmpTime;
                    FunctionDatas[methodName] = tmp;
                }
                else
                {
                    var tmp = new FuncData
                    {
                        FuncName = methodName,
                        FuncMemory = 0L,
                        FuncTime = 0f,
                        FuncCalls = 0,
                        FuncTotalMemory = 0L,
                        FuncTotalTime = 0f,
                        BeginMemory = tmpMemory,
                        BeginTime = tmpTime
                    };
                    FunctionDatas.Add(methodName, tmp);
                }
            }
        }

        public static void End(string methodName)
        {
            if (!UProfilerSettings.IsFunctionHookEnabled)
                return;

            if (Thread.CurrentThread == MainThread)
            {
                if (!FunctionDatas.ContainsKey(methodName))
                    return;

                long tmpMem = Profiler.GetTotalAllocatedMemoryLong();
                float tmpTime = Time.realtimeSinceStartup;
                FuncData tmp = FunctionDatas[methodName];
                // Ignore negative deltas caused by GC during the sample.
                if (tmpMem - tmp.BeginMemory >= 0)
                {
                    tmp.FuncMemory = tmpMem - tmp.BeginMemory;
                    tmp.FuncTime = tmpTime - tmp.BeginTime;
                    tmp.FuncTotalMemory += tmp.FuncMemory;
                    tmp.FuncTotalTime += tmp.FuncTime;
                    tmp.FuncCalls += 1;
                    tmp.BeginMemory = 0L;
                    tmp.BeginTime = 0f;
                    FunctionDatas[methodName] = tmp;
                }
            }
        }

        public static void MethodAnalysisReport(string testTime = "")
        {
            if (!UProfilerSettings.IsFunctionHookEnabled)
            {
                Debug.LogWarning("[UProfiler] Function Hook is disabled. Enable it in UProfiler > Settings.");
                return;
            }

            if (FunctionDatas.Count <= 0)
            {
                Debug.Log("IL inject succeeded; no samples collected yet.");
                return;
            }
            string fileCsvName = "";
            string fileTxtName = "";
            if (string.IsNullOrEmpty(testTime))
            {
                fileCsvName = System.DateTime.Now.ToString("[yyyy-MM-dd]-[HH-mm-ss]");
            }
            else
            {
                fileCsvName = ConstString.funcAnalysisPrefix + testTime;
                fileTxtName = ConstString.funcAnalysisPrefix + testTime + ConstString.textExt;
                fileTxtName = Path.Combine(Application.persistentDataPath, fileTxtName);
            }
            fileCsvName += ".csv";
            fileCsvName = Path.Combine(Application.persistentDataPath, fileCsvName);

            string header = "FuncName,FuncMemory/k,FuncAverageMemory/k,FuncUseTime/s,FuncAverageTime/ms,FuncCalls";
            using (StreamWriter sw = new StreamWriter(fileCsvName))
            {
                sw.WriteLine(header);
                using var ge = FunctionDatas.GetEnumerator();
                while (ge.MoveNext())
                {
                    var tmp = ge.Current.Value;
                    if (tmp.FuncCalls <= 0) continue;
                    StringBuilder sb = new StringBuilder();
                    sb.AppendFormat("{0},", tmp.FuncName);
                    sb.AppendFormat("{0:f4},", tmp.FuncMemory / 1024.0);
                    sb.AppendFormat("{0:f4},", tmp.FuncTotalMemory / (tmp.FuncCalls * 1024.0));
                    sb.AppendFormat("{0},", tmp.FuncTime);
                    sb.AppendFormat("{0},", tmp.FuncTotalTime / tmp.FuncCalls * 1000);
                    sb.AppendFormat("{0}", tmp.FuncCalls);
                    sw.WriteLine(sb);
                }
                sw.Close();
            }
            Debug.Log($"Function performance CSV written: {fileCsvName}");

            if (!string.IsNullOrEmpty(fileTxtName))
            {
                List<FuncAnalysisInfo> funcAnalysisInfos = new List<FuncAnalysisInfo>();
                using var ge = FunctionDatas.GetEnumerator();
                while (ge.MoveNext())
                {
                    var tmp = ge.Current.Value;
                    if (tmp.FuncCalls <= 0) continue;
                    FuncAnalysisInfo funcInfo = new FuncAnalysisInfo()
                    {
                        name = tmp.FuncName,
                        memory = tmp.FuncMemory / 1024.0,
                        averageMemory = tmp.FuncTotalMemory / (tmp.FuncCalls * 1024.0),
                        useTime = tmp.FuncTime,
                        averageTime = tmp.FuncTotalTime / tmp.FuncCalls * 1000,
                        calls = tmp.FuncCalls
                    };
                    funcAnalysisInfos.Add(funcInfo);
                }

                var res = FileManager.WriteToFile(fileTxtName, Newtonsoft.Json.JsonConvert.SerializeObject(funcAnalysisInfos));
                if (res)
                    Debug.Log($"Function performance JSON written: {fileTxtName}");
                else
                    Debug.LogError($"Failed to write function performance JSON: {fileTxtName}");
            }
        }

        public static void BeginSample(string methodName)
        {
            Profiler.BeginSample(methodName);
        }

        public static void EndSample()
        {
            Profiler.EndSample();
        }

        public static void BeginDebugLog(string methodName)
        {
            if (!UProfilerSettings.IsFunctionHookEnabled)
                return;

            Debug.Log($"---Hook BeginDebugLog:{methodName}");
        }

        public static void EndDebugLog(string methodName)
        {
            if (!UProfilerSettings.IsFunctionHookEnabled)
                return;

            Debug.Log($"---Hook EndDebugLog:{methodName}");
        }

        public static void PrintMethodDatas()
        {
            if (!UProfilerSettings.IsFunctionHookEnabled)
            {
                Debug.LogWarning("[UProfiler] Function Hook is disabled. Enable it in UProfiler > Settings.");
                return;
            }

            if (FunctionDatas.Count <= 0)
            {
                Debug.LogWarning(
                    "[UProfiler] No function timing data collected.\n" +
                    "• UProfiler → Settings: Enable Function Hook must be ON\n" +
                    "• Enter Play mode (auto-injects [FunctionAnalysis] before Play)\n" +
                    "• Or manually: Hook → Inject [FunctionAnalysis] Methods, then Play\n" +
                    "• ProfilerSample inject does NOT fill this list; [FunctionAnalysis] methods must run first");
                return;
            }
            Debug.Log("------------ Method timing summary -----------------");
            using var ge = FunctionDatas.GetEnumerator();
            while (ge.MoveNext())
            {
                var tmp = ge.Current.Value;
                if (tmp.FuncCalls <= 0) continue;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("{0},", tmp.FuncName);
                sb.AppendFormat("{0:f4},", tmp.FuncMemory / 1024.0);
                sb.AppendFormat("{0:f4},", tmp.FuncTotalMemory / (tmp.FuncCalls * 1024.0));
                sb.AppendFormat("{0},", tmp.FuncTime);
                sb.AppendFormat("{0},", tmp.FuncTotalTime / tmp.FuncCalls);
                sb.AppendFormat("{0}", tmp.FuncCalls);
                Debug.Log(sb.ToString());
            }
        }
#else
        public static void Begin(string methodName) { }
        public static void End(string methodName) { }
        public static void MethodAnalysisReport(string testTime = "") { }
        public static void BeginSample(string methodName) { }
        public static void EndSample() { }
        public static void BeginDebugLog(string methodName) { }
        public static void EndDebugLog(string methodName) { }
        public static void PrintMethodDatas() { }
#endif
    }
}
