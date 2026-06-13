using System.Collections.Generic;

namespace LemonFramework.UProfiler.Core
{
    public static class Config
    {
        /// <summary>Server host. Default <c>localhost</c> for Editor; set to your LAN IP for Android device builds.</summary>
        public static string IP = "localhost";

        public static int Port = 8080;

        public static string BaseUrl => $"http://{IP}:{Port}";
        public static string ReportUrl => BaseUrl;

        public static string ReportRecordUpdateRequestUrl =>
            $"{BaseUrl}/ReceiveDataHandler.ashx?PackageName={{0}}&TestTime={{1}}";

        public static string PostFileUrl => $"{BaseUrl}/TestHandler.ashx";
        public static bool UseFtpUpload = false;

        /// <summary>可选的 HTTP 上传请求头。本地 UProfiler-Server 无需配置。</summary>
        public static Dictionary<string, string> PostFileHeaders = new();
    }

    public static class ConstString
    {
        // 配置语言
        public const string uProfilerBegin = "开始监控";
        public const string uProfilerActive = "监控中";
        public const string uProfilerStop = "停止监控";
        public const string binaryExt = ".data";
        public const string textExt = ".txt";

        // 文件前缀
        public const string frameRatefix = "frameRate_";
        public const string logPrefix = "log_";
        public const string devicePrefix = "device_";
        public const string testPrefix = "test_";

        public const string uProfilerPrefix = "uprofiler_";

        // 函数性能分析
        public const string funcAnalysisPrefix = "funcAnalysis_";

        // 函数规划规范分析
        public const string funcCodeAnalysisPrefix = "funcCodeAnalysis_";

        public const string cpuTemperaturePrefix = "cpuTemperature_";

        // 功耗模块
        public const string powerConsumePrefix = "powerConsume_";

        // 采集帧率
        public const string captureFramePrefix = "captureFrame_";

        // 内存分布
        public const string resMemoryDistributionPrefix = "resMemoryDistribution_";

        // 渲染信息
        public const string renderPrefix = "renderInfo_";
        public const string pssMemoryPrefix = "pssMemoryInfo_";
        public const string sceneInfoPrefix = "sceneInfo_";
        public const string moduleTimePrefix = "moduleTime_";
        public const string threadStackPrefix = "threadStack_";
        public const string moduleFuncStackPrefix = "moduleFuncStack_";
        public const string briefAiDiagnosisPrefix = "briefAiDiagnosis_";
        public const string hardwareInfoPrefix = "hardwareInfo_";
        public const string gpuBandwidthPrefix = "gpuBandwidth_";
        public const string luaMemoryPrefix = "luaMemory_";
        public const string resourceManagementPrefix = "resourceManagement_";
        public const string customDashboardPrefix = "customDashboard_";
        public const string apiFuncsPrefix = "apiFuncs_";
        public const string apiInfoPrefix = "apiInfo_";
        public const string apiCodeFramePrefix = "apiCodeFrame_";
    }
}