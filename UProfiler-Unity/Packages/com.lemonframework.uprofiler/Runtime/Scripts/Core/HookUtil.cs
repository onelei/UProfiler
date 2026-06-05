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
        static Thread mainThread = Thread.CurrentThread;
        static Dictionary<string, FuncData> FunctionDatas = new Dictionary<string, FuncData>();

        public static void Begin(string methodName)
        {
            // Only sample on the Unity main thread.
            if (Thread.CurrentThread == mainThread)
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
                    var tmp = new FuncData();
                    tmp.FuncName = methodName;
                    tmp.FuncMemory = 0L;
                    tmp.FuncTime = 0f;
                    tmp.FuncCalls = 0;
                    tmp.FuncTotalMemory = 0L;
                    tmp.FuncTotalTime = 0f;
                    tmp.BeginMemory = tmpMemory;
                    tmp.BeginTime = tmpTime;
                    FunctionDatas.Add(methodName, tmp);
                }
            }
        }

        public static void End(string methodName)
        {
            if (Thread.CurrentThread == mainThread)
            {
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
            if (FunctionDatas.Count <= 0)
            {
                Debug.Log("IL inject succeeded; no samples collected yet.");
                return;
            }
            string fileCSVName = "";
            string fileTxtName = "";
            if (string.IsNullOrEmpty(testTime))
            {
                fileCSVName = System.DateTime.Now.ToString("[yyyy-MM-dd]-[HH-mm-ss]");
            }
            else
            {
                fileCSVName = ConstString.funcAnalysisPrefix + testTime;
                fileTxtName = ConstString.funcAnalysisPrefix + testTime + ConstString.textExt;
                fileTxtName = Path.Combine(Application.persistentDataPath, fileTxtName);
            }
            fileCSVName += ".csv";
            fileCSVName = Path.Combine(Application.persistentDataPath, fileCSVName);

            string header = "FuncName,FuncMemory/k,FuncAverageMemory/k,FuncUseTime/s,FuncAverageTime/ms,FuncCalls";
            using (StreamWriter sw = new StreamWriter(fileCSVName))
            {
                sw.WriteLine(header);
                var ge = FunctionDatas.GetEnumerator();
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
            Debug.Log($"Function performance CSV written: {fileCSVName}");

            if (!string.IsNullOrEmpty(fileTxtName))
            {
                List<FuncAnalysisInfo> funcAnalysisInfos = new List<FuncAnalysisInfo>();
                var ge = FunctionDatas.GetEnumerator();
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
            Debug.Log($"---Hook BeginDebugLog:{methodName}");
        }

        public static void EndDebugLog(string methodName)
        {
            Debug.Log($"---Hook EndDebugLog:{methodName}");
        }

        public static void PrintMethodDatas()
        {
            if (FunctionDatas.Count <= 0)
            {
                Debug.LogWarning("Run a Hook inject menu item first, then profile and print again.");
                return;
            }
            Debug.Log("------------ Method timing summary -----------------");
            var ge = FunctionDatas.GetEnumerator();
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
    }
}
