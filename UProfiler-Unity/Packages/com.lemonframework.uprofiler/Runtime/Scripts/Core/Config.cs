using System.Collections.Generic;

namespace LemonFramework.UProfiler.Core
{
    public class Config
    {
        /// <summary>Server host. Default <c>localhost</c> for Editor; set to your LAN IP for Android device builds.</summary>
        public static string IP = "localhost";
        public static int Port = 8080;

        public static string BaseUrl => $"http://{IP}:{Port}";
        public static string ReportUrl => BaseUrl;
        public static string ReportRecordUpdateRequestUrl => $"{BaseUrl}/ReceiveDataHandler.ashx?PackageName={{0}}&TestTime={{1}}";
        public static string PostFileUrl => $"{BaseUrl}/TestHandler.ashx";
        public static bool UseFtpUpload = false;
        public static Dictionary<string, string> PostFileHeaders = new Dictionary<string, string>() { { "X-HW-ID", "com.huawei.xr.cyberverse.cybersim" }, { "X-HW-APPKEY", "bA2J8D1u9djyOVtS8efNTQ==" } };
    }

    public class ConstString
    {
        // 配置语言
        public const string UProfilerBegin = "开始监控";
        public const string UProfilerActive = "监控中";
        public const string UProfilerStop = "停止监控";
        public const string BinaryExt = ".data";
        public const string TextExt = ".txt";

        // 文件前缀
        public const string FrameRatefix = "frameRate_";
        public const string LogPrefix = "log_";
        public const string DevicePrefix = "device_";
        public const string TestPrefix = "test_";
        public const string UProfilerPrefix = "uprofiler_";
        // 函数性能分析
        public const string FuncAnalysisPrefix = "funcAnalysis_";
        // 函数规划规范分析
        public const string FuncCodeAnalysisPrefix = "funcCodeAnalysis_";
        public const string CPUTemperaturePrefix = "cpuTemperature_";
        // 功耗模块
        public const string PowerConsumePrefix = "powerConsume_";

        // 采集帧率
        public const string CaptureFramePrefix = "captureFrame_";
        // 内存分布
        public const string ResMemoryDistributionPrefix = "resMemoryDistribution_";
        // 渲染信息
        public const string RenderPrefix = "renderInfo_";
        public const string PssMemoryPrefix = "pssMemoryInfo_";
    }
}