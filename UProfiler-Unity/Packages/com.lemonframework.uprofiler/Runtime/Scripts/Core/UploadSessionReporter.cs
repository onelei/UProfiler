using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    public static class UploadSessionReporter
    {
        public static void WriteExtendedReports(
            string sessionKey,
            string fileExt,
            int totalFrames,
            bool enableModuleTime,
            bool enableHardwareInfo,
            bool enableExtendedReports,
            bool enableLuaMemory,
            ModuleTimeSampler moduleTimeSampler,
            HardwareInfoSampler hardwareInfoSampler,
            RenderInfos renderInfos,
            out List<string> filesToUpload)
        {
            filesToUpload = new List<string>();
            var baseDir = Application.persistentDataPath;
            var funcAnalysis = ResolveFuncAnalysis(moduleTimeSampler);
            var hasModuleTime = enableModuleTime && moduleTimeSampler != null && moduleTimeSampler.Data.x.Count > 0;

            if (hasModuleTime)
            {
                moduleTimeSampler.FinalizeSummary();
                var path = Path.Combine(baseDir, $"{ConstString.moduleTimePrefix}{sessionKey}{fileExt}");
                if (ProfileReportExporter.WriteJson(path, moduleTimeSampler.Data))
                {
                    filesToUpload.Add(path);
                }
            }

            if (enableHardwareInfo && hardwareInfoSampler != null && hardwareInfoSampler.Data.samples.Count > 0)
            {
                var path = Path.Combine(baseDir, $"{ConstString.hardwareInfoPrefix}{sessionKey}{fileExt}");
                if (ProfileReportExporter.WriteJson(path, hardwareInfoSampler.Data))
                {
                    filesToUpload.Add(path);
                }
            }

            if (ResourceEventTracker.HasEvents)
            {
                var path = Path.Combine(baseDir, $"{ConstString.resourceManagementPrefix}{sessionKey}{fileExt}");
                var payload = ResourceEventTracker.BuildPayload(totalFrames);
                if (ProfileReportExporter.WriteJson(path, payload))
                {
                    filesToUpload.Add(path);
                    Debug.Log($"[UProfiler] 已生成 resourceManagement_（{payload.assetBundle.Count + payload.resource.Count + payload.instantiate.Count} 条事件）。");
                }
            }

            if (renderInfos != null && renderInfos.renderInfoList.Count > 0)
            {
                var gpuPath = Path.Combine(baseDir, $"{ConstString.gpuBandwidthPrefix}{sessionKey}{fileExt}");
                if (ProfileReportExporter.WriteJson(gpuPath, ProfileReportExporter.BuildGpuBandwidthEstimate(renderInfos)))
                {
                    filesToUpload.Add(gpuPath);
                }
            }

            if (enableLuaMemory)
            {
                var luaPayload = LuaMemoryCollector.BuildPayload();
                if (luaPayload != null)
                {
                    var luaPath = Path.Combine(baseDir, $"{ConstString.luaMemoryPrefix}{sessionKey}{fileExt}");
                    if (ProfileReportExporter.WriteJson(luaPath, luaPayload))
                    {
                        filesToUpload.Add(luaPath);
                    }
                }
            }

            if (enableExtendedReports && (hasModuleTime || funcAnalysis.Count > 0))
            {
                var moduleTimeData = hasModuleTime ? moduleTimeSampler.Data : null;
                if (hasModuleTime)
                {
                    var threadStackPath = Path.Combine(baseDir, $"{ConstString.threadStackPrefix}{sessionKey}{fileExt}");
                    var threadStack = ProfileReportExporter.BuildThreadStack(moduleTimeData, funcAnalysis, totalFrames);
                    if (ProfileReportExporter.WriteJson(threadStackPath, threadStack))
                    {
                        filesToUpload.Add(threadStackPath);
                    }

                    var briefPath = Path.Combine(baseDir, $"{ConstString.briefAiDiagnosisPrefix}{sessionKey}{fileExt}");
                    var brief = ProfileReportExporter.BuildBriefAiDiagnosis(moduleTimeData, renderInfos);
                    if (ProfileReportExporter.WriteJson(briefPath, brief))
                    {
                        filesToUpload.Add(briefPath);
                    }
                }

                if (funcAnalysis.Count > 0)
                {
                    foreach (var (moduleKey, stack) in ProfileReportExporter.BuildModuleFuncStacks(
                                 moduleTimeData, funcAnalysis, renderInfos, totalFrames))
                    {
                        var stackPath = Path.Combine(
                            baseDir,
                            $"{ConstString.moduleFuncStackPrefix}{moduleKey}_{sessionKey}{fileExt}");
                        if (ProfileReportExporter.WriteJson(stackPath, stack))
                        {
                            filesToUpload.Add(stackPath);
                        }
                    }
                }
            }

            WriteCustomReports(baseDir, sessionKey, fileExt, filesToUpload);
        }

        static List<FuncAnalysisInfo> ResolveFuncAnalysis(ModuleTimeSampler moduleTimeSampler)
        {
            var funcAnalysis = HookUtil.BuildFuncAnalysisList();
            if (funcAnalysis.Count > 0)
            {
                return funcAnalysis;
            }

            if (moduleTimeSampler != null && moduleTimeSampler.IsSupported && moduleTimeSampler.Data.x.Count > 0)
            {
                funcAnalysis = moduleTimeSampler.BuildSyntheticFuncAnalysis();
                if (funcAnalysis.Count > 0)
                {
                    Debug.Log("[UProfiler] 未采集到 Hook 函数数据，已使用 Profiler 模块采样生成 moduleFuncStack_。");
                }
            }

            return funcAnalysis;
        }

        static void WriteCustomReports(string baseDir, string sessionKey, string fileExt, List<string> filesToUpload)
        {
            if (CustomDataTracker.HasDashboardData)
            {
                var path = Path.Combine(baseDir, $"{ConstString.customDashboardPrefix}{sessionKey}{fileExt}");
                if (ProfileReportExporter.WriteJson(path, CustomDataTracker.BuildDashboardPayload()))
                {
                    filesToUpload.Add(path);
                }
            }

            if (CustomDataTracker.HasFuncsData)
            {
                var path = Path.Combine(baseDir, $"{ConstString.apiFuncsPrefix}{sessionKey}{fileExt}");
                if (ProfileReportExporter.WriteJson(path, CustomDataTracker.BuildFuncsPayload()))
                {
                    filesToUpload.Add(path);
                }
            }

            if (CustomDataTracker.HasVarsData)
            {
                var path = Path.Combine(baseDir, $"{ConstString.apiInfoPrefix}{sessionKey}{fileExt}");
                if (ProfileReportExporter.WriteJson(path, CustomDataTracker.BuildVarsPayload()))
                {
                    filesToUpload.Add(path);
                }
            }

            if (CustomDataTracker.HasCodeData)
            {
                var path = Path.Combine(baseDir, $"{ConstString.apiCodeFramePrefix}{sessionKey}{fileExt}");
                if (ProfileReportExporter.WriteJson(path, CustomDataTracker.BuildCodePayload()))
                {
                    filesToUpload.Add(path);
                }
            }
        }
    }
}
