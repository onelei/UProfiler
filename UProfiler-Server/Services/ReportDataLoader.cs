using System.Globalization;
using System.Text.Json;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ReportDataLoader
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static ReportDataContext Load(string sessionKey, string? packageName, IReadOnlyList<SessionUpload> files)
    {
        var moduleTimeUploaded = ReadJson<ModuleTimePayload>(files, "moduleTime");
        var testInfo = ReadJson<TestInfoDto>(files, "test");
        var deviceInfo = ReadJson<DeviceInfoDto>(files, "device");
        var frameRates = ReadJson<FrameRatesDto>(files, "frameRate");
        var uprofilerInfos = ReadJson<UProfilerInfosDto>(files, "uprofiler")
            ?? ReadJson<UProfilerInfosDto>(files, "monitor");
        var renderInfos = ReadJson<RenderInfosDto>(files, "renderInfo");
        var memoryUse = ReadJson<MemoryUseDatasDto>(files, "pssMemoryInfo");
        var powerInfos = ReadJson<DevicePowerConsumeInfosDto>(files, "powerConsume");
        var resourceMemory = ReadJson<RecordResInfosDto>(files, "resMemoryDistribution");
        var sceneInfo = ReadJson<SceneInfoDto>(files, "sceneInfo");
        var threadStackRaw = ReadJson<ThreadStackPayload>(files, "threadStack");
        var briefAiDiagnosis = ReadJson<BriefAiDiagnosisDto>(files, "briefAiDiagnosis");
        var hardwareInfo = ReadJson<HardwareInfoDto>(files, "hardwareInfo");
        var gpuBandwidth = ReadJson<GpuBandwidthDto>(files, "gpuBandwidth");
        var luaMemory = ReadJson<LuaMemoryDto>(files, "luaMemory");
        var resourceManagement = ReadJson<ResourceManagementDto>(files, "resourceManagement");
        var customDashboard = ReadJson<CustomDashboardDto>(files, "customDashboard");
        var customFuncs = ReadJson<CustomFuncsDto>(files, "apiFuncs");
        var customVars = ReadJson<CustomVarsDto>(files, "apiInfo");
        var customCode = ReadJson<CustomCodeDto>(files, "apiCodeFrame");
        var moduleFuncStacks = ReadModuleFuncStacks(files);
        var funcAnalysis = ReadJsonList<FuncAnalysisInfoDto>(files, "funcAnalysis")
            .OrderByDescending(item => item.AverageTime)
            .ToList();
        var logLines = ReadLogLines(files);

        var fpsValues = frameRates?.FrameRateList.Select(item => item.Frame).ToList() ?? new List<int>();
        var avgFps = fpsValues.Count > 0 ? fpsValues.Average() : 0;
        var minFps = fpsValues.Count > 0 ? fpsValues.Min() : 0;
        var maxFps = fpsValues.Count > 0 ? fpsValues.Max() : 0;

        var monitorList = uprofilerInfos?.GetAll() ?? new List<UProfilerInfoDto>();
        var renderList = renderInfos?.RenderInfoList ?? new List<RenderInfoDto>();
        var powerList = powerInfos?.DevicePowerConsumeInfos ?? new List<DevicePowerConsumeInfoDto>();
        var pssList = memoryUse?.MemoryUsedList ?? new List<MemoryUseDataDto>();

        var captureFrames = CaptureFrameService.Build(sessionKey, files);
        var context = new ReportDataContext
        {
            SessionKey = sessionKey,
            PackageName = packageName,
            TestInfo = testInfo,
            DeviceInfo = deviceInfo,
            FrameRates = frameRates,
            UProfilerInfos = uprofilerInfos,
            RenderInfos = renderInfos,
            MemoryUseDatas = memoryUse,
            PowerInfos = powerInfos,
            ResourceMemory = resourceMemory,
            SceneInfo = sceneInfo,
            ThreadStack = threadStackRaw ?? new ThreadStackPayload(),
            BriefAiDiagnosis = briefAiDiagnosis,
            HardwareInfo = hardwareInfo,
            GpuBandwidth = gpuBandwidth,
            LuaMemory = luaMemory,
            ResourceManagement = resourceManagement,
            ModuleFuncStacks = moduleFuncStacks,
            CustomDashboard = customDashboard,
            CustomFuncs = customFuncs,
            CustomVars = customVars,
            CustomCode = customCode,
            FuncAnalysis = funcAnalysis,
            LogLines = logLines,
            Files = files,
            CaptureFrames = captureFrames,
            AvgFps = avgFps,
            MinFps = minFps,
            MaxFps = maxFps,
            PeakMonoUsed = monitorList.Count > 0 ? monitorList.Max(item => item.MonoUsedSize) : 0,
            PeakTotalAllocated = monitorList.Count > 0 ? monitorList.Max(item => item.TotalAllocatedMemory) : 0,
            PeakDrawCall = renderList.Count > 0 ? renderList.Max(item => item.DrawCall) : 0,
            PeakTriangles = renderList.Count > 0 ? renderList.Max(item => item.Triangles) : 0,
            PeakPssMb = pssList.Count > 0 ? pssList.Max(item => item.PssMemorySize) : 0,
            PeakBatteryPower = powerList.Count > 0 ? powerList.Max(item => item.BatteryPower) : 0,
            PeakCpuTemp = powerList.Count > 0 ? powerList.Max(item => item.CpuTemperate) : 0,
            DiagnosisItems = ReportDiagnosisEngine.Build(
                avgFps,
                minFps,
                maxFps,
                monitorList,
                renderList,
                funcAnalysis,
                pssList,
                powerList)
        };

        var moduleTime = moduleTimeUploaded != null && moduleTimeUploaded.X.Count > 0
            ? ModuleTimeBuilder.EnrichUploaded(moduleTimeUploaded)
            : ModuleTimeBuilder.Build(context);
        var enriched = context with
        {
            ModuleTime = moduleTime,
            ModuleDetails = ModuleDetailBuilder.Build(context with { ModuleTime = moduleTime }),
            Jank = JankAnalyzer.Build(context),
            ResourceSummary = BuildResourceSummary(resourceMemory),
            JankFuncCategories = JankAnalyzer.BuildCategories(context)
        };

        var withScene = enriched with
        {
            Brief = ReportBriefBuilder.Build(enriched, enriched.Jank),
            SceneManagement = SceneInfoBuilder.Build(enriched)
        };

        var withThread = withScene with
        {
            ThreadStack = ThreadStackBuilder.Build(withScene)
        };

        var collapseMetrics = BriefCollapseBuilder.Build(withThread);
        var finalBrief = new PerformanceBriefPayload
        {
            FrameCount = withThread.Brief.FrameCount,
            SummaryText = withThread.Brief.SummaryText,
            OptimizableCount = collapseMetrics.Count(item => item.TaskCount > 0),
            TotalMetricCount = collapseMetrics.Count,
            Kpis = withThread.Brief.Kpis,
            Metrics = collapseMetrics
        };

        return withThread with { Brief = finalBrief };
    }

    static Dictionary<string, ModuleFuncStackDto> ReadModuleFuncStacks(IReadOnlyList<SessionUpload> files)
    {
        var result = new Dictionary<string, ModuleFuncStackDto>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in files.Where(item =>
                     item.Prefix.Equals("moduleFuncStack", StringComparison.OrdinalIgnoreCase)
                     && item.OriginalName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                var stack = JsonSerializer.Deserialize<ModuleFuncStackDto>(File.ReadAllText(file.SavedPath), JsonOptions);
                if (stack != null && !string.IsNullOrWhiteSpace(stack.Module))
                {
                    result[stack.Module] = stack;
                }
            }
            catch
            {
                // ignore malformed uploads
            }
        }

        return result;
    }

    static List<ResourceSummaryRow> BuildResourceSummary(RecordResInfosDto? resourceMemory)
    {
        var list = resourceMemory?.RecordResInfosList ?? new List<RecordResInfoDto>();
        if (list.Count == 0)
        {
            return new List<ResourceSummaryRow>();
        }

        return new List<ResourceSummaryRow>
        {
            BuildResRow("Texture", list, item => item.TextureSize, item => item.TextureCount),
            BuildResRow("Mesh", list, item => item.MeshSize, item => item.MeshCount),
            BuildResRow("Material", list, item => item.MaterialSize, item => item.MaterialCount),
            BuildResRow("Shader", list, item => item.ShaderSize, item => item.ShaderCount),
            BuildResRow("AnimationClip", list, item => item.AnimationClipSize, item => item.AnimationClipCount),
            BuildResRow("AudioClip", list, item => item.AudioClipSize, item => item.AudioClipCount),
            BuildResRow("Font", list, item => item.FontSize, item => item.FontCount),
            BuildResRow("TextAsset", list, item => item.TextAssetSize, item => item.TextAssetCount),
            BuildResRow("ScriptableObject", list, item => item.ScriptableObjectSize, item => item.ScriptableObjectCount)
        };
    }

    static readonly Dictionary<string, (string Label, string Recommend)> ResourceLabelMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Texture"] = ("纹理资源", "210 MB"),
        ["Mesh"] = ("网格资源", "75 MB"),
        ["Material"] = ("材质资源", "2 MB"),
        ["Shader"] = ("Shader资源", "50 MB"),
        ["AnimationClip"] = ("动画片段", "60 MB"),
        ["AudioClip"] = ("音频片段", "30 MB"),
        ["Font"] = ("字体资源", "20 MB"),
        ["TextAsset"] = ("TextAsset", "30 MB"),
        ["ScriptableObject"] = ("其他", "-")
    };

    static ResourceSummaryRow BuildResRow(
        string type,
        List<RecordResInfoDto> list,
        Func<RecordResInfoDto, long> sizeSelector,
        Func<RecordResInfoDto, int> countSelector)
    {
        ResourceLabelMap.TryGetValue(type, out var meta);
        return new ResourceSummaryRow
        {
            Type = type,
            Label = string.IsNullOrWhiteSpace(meta.Label) ? type : meta.Label,
            RecommendText = string.IsNullOrWhiteSpace(meta.Recommend) ? "-" : meta.Recommend,
            AvgSizeBytes = (long)list.Average(item => sizeSelector(item)),
            AvgCount = (int)list.Average(item => countSelector(item)),
            PeakSizeBytes = list.Max(sizeSelector)
        };
    }

    static T? ReadJson<T>(IReadOnlyList<SessionUpload> files, string prefix) where T : class
    {
        var file = files.FirstOrDefault(item =>
            item.Prefix.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            && item.OriginalName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

        if (file == null || !File.Exists(file.SavedPath))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(File.ReadAllText(file.SavedPath), JsonOptions);
        }
        catch
        {
            return null;
        }
    }

    static List<T> ReadJsonList<T>(IReadOnlyList<SessionUpload> files, string prefix)
    {
        var file = files.FirstOrDefault(item =>
            item.Prefix.Equals(prefix, StringComparison.OrdinalIgnoreCase)
            && item.OriginalName.EndsWith(".txt", StringComparison.OrdinalIgnoreCase));

        if (file == null || !File.Exists(file.SavedPath))
        {
            return new List<T>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(file.SavedPath), JsonOptions) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    static List<string> ReadLogLines(IReadOnlyList<SessionUpload> files)
    {
        var file = files.FirstOrDefault(item => item.Prefix.Equals("log", StringComparison.OrdinalIgnoreCase));
        if (file == null || !File.Exists(file.SavedPath))
        {
            return new List<string>();
        }

        if (file.OriginalName.EndsWith(".data", StringComparison.OrdinalIgnoreCase))
        {
            return new List<string> { "[Binary log files are not parsed in local reports yet.]" };
        }

        return File.ReadLines(file.SavedPath).Take(500).ToList();
    }
}

public static class ReportDiagnosisEngine
{
    public static List<DiagnosisItem> Build(
        double avgFps,
        int minFps,
        int maxFps,
        List<UProfilerInfoDto> monitorList,
        List<RenderInfoDto> renderList,
        List<FuncAnalysisInfoDto> funcAnalysis,
        List<MemoryUseDataDto> pssList,
        List<DevicePowerConsumeInfoDto> powerList)
    {
        var items = new List<DiagnosisItem>();
        AddFpsItems(items, avgFps, minFps, maxFps);
        AddRenderItems(items, renderList);
        AddMemoryItems(items, monitorList, pssList);
        AddFuncItems(items, funcAnalysis);
        AddPowerItems(items, powerList);
        return items;
    }

    static void AddFpsItems(List<DiagnosisItem> items, double avgFps, int minFps, int maxFps)
    {
        if (avgFps <= 0)
        {
            return;
        }

        items.Add(new DiagnosisItem
        {
            Id = "fps-avg",
            Category = "整体",
            Title = "平均帧率",
            Severity = avgFps < 25 ? "HIGH" : avgFps < 45 ? "MEDIUM" : "LOW",
            ValueText = $"{avgFps:F1} FPS",
            IndustryText = "行业水平 30 FPS",
            RecommendText = "UWA 推荐值 \u2265 30 FPS",
            Summary = $"本次测试平均帧率 {avgFps:F1} FPS，最低 {minFps} FPS，最高 {maxFps} FPS。",
            Suggestions = new List<string>
            {
                "检查 CPU 主线程耗时，关注 Update/FixedUpdate 中的重逻辑。",
                "结合函数性能表定位平均耗时超过 15ms 的热点函数。",
                "在目标设备上关闭不必要的日志输出后再对比帧率。"
            }
        });

        if (minFps < 30)
        {
            items.Add(new DiagnosisItem
            {
                Id = "fps-min",
                Category = "整体",
                Title = "最低帧率",
                Severity = minFps < 20 ? "HIGH" : "MEDIUM",
                ValueText = $"{minFps} FPS",
                IndustryText = "行业水平 25 FPS",
                RecommendText = "UWA 推荐值 \u2265 25 FPS",
                Summary = $"测试过程中最低帧率为 {minFps} FPS，存在明显卡顿风险。",
                Suggestions = new List<string>
                {
                    "定位低帧率时段对应的场景与操作。",
                    "检查是否存在瞬时大量 Instantiate 或同步加载。",
                    "关注渲染模块 DrawCall / 三角面峰值是否与低帧率同步。"
                }
            });
        }
    }

    static void AddRenderItems(List<DiagnosisItem> items, List<RenderInfoDto> renderList)
    {
        if (renderList.Count == 0)
        {
            return;
        }

        var peakDc = renderList.Max(item => item.DrawCall);
        var peakTri = renderList.Max(item => item.Triangles);
        items.Add(new DiagnosisItem
        {
            Id = "render-dc",
            Category = "渲染模块",
            Title = "DrawCall 峰值",
            Severity = peakDc > 300 ? "HIGH" : peakDc > 200 ? "MEDIUM" : "LOW",
            ValueText = peakDc.ToString(CultureInfo.InvariantCulture),
            IndustryText = "行业水平 200",
            RecommendText = "UWA 推荐值 \u2264 200",
            Summary = $"DrawCall 峰值 {peakDc}，三角面峰值 {peakTri}。",
            Suggestions = new List<string>
            {
                "合并材质与贴图，减少 SetPassCall / DrawCall。",
                "检查 UI 是否开启过多 Canvas 或频繁重建布局。",
                "对静态场景启用 Static Batching / GPU Instancing。"
            }
        });
    }

    static void AddMemoryItems(
        List<DiagnosisItem> items,
        List<UProfilerInfoDto> monitorList,
        List<MemoryUseDataDto> pssList)
    {
        if (monitorList.Count > 0)
        {
            var peakMono = monitorList.Max(item => item.MonoUsedSize);
            var peakTotal = monitorList.Max(item => item.TotalAllocatedMemory);
            items.Add(new DiagnosisItem
            {
                Id = "mem-mono",
                Category = "内存",
                Title = "Mono 堆内存峰值",
                Severity = peakMono > 150 * 1024 * 1024 ? "HIGH" : peakMono > 80 * 1024 * 1024 ? "MEDIUM" : "LOW",
                ValueText = FormatBytes(peakMono),
                IndustryText = "行业水平 80MB",
                RecommendText = "UWA 推荐值 \u2264 80MB",
                Summary = $"MonoUsed 峰值 {FormatBytes(peakMono)}，TotalAllocated 峰值 {FormatBytes(peakTotal)}。",
                Suggestions = new List<string>
                {
                    "减少每帧 new 容器/字符串，避免不必要的装箱。",
                    "检查事件订阅、静态缓存是否在场景切换后释放。",
                    "使用 Memory Profiler 定位托管堆持续增长对象。"
                }
            });
        }

        if (pssList.Count > 0)
        {
            var peakPss = pssList.Max(item => item.PssMemorySize);
            items.Add(new DiagnosisItem
            {
                Id = "mem-pss",
                Category = "内存",
                Title = "PSS 内存峰值",
                Severity = peakPss > 1200 ? "HIGH" : peakPss > 800 ? "MEDIUM" : "LOW",
                ValueText = $"{peakPss:F1} MB",
                IndustryText = "行业水平 800MB",
                RecommendText = "UWA 推荐值 \u2264 800MB",
                Summary = $"Android PSS 峰值 {peakPss:F1} MB。",
                Suggestions = new List<string>
                {
                    "检查 Texture / Mesh / Audio 资源是否存在冗余。",
                    "降低超大贴图分辨率，合理使用 ASTC/ETC 压缩。",
                    "场景切换时及时卸载未使用 AssetBundle / 资源。"
                }
            });
        }
    }

    static void AddFuncItems(List<DiagnosisItem> items, List<FuncAnalysisInfoDto> funcAnalysis)
    {
        var hot = funcAnalysis.FirstOrDefault(item => item.AverageTime >= 15f);
        if (hot == null)
        {
            return;
        }

        items.Add(new DiagnosisItem
        {
            Id = "func-hot",
            Category = "逻辑脚本",
            Title = "高耗时函数",
            Severity = hot.AverageTime >= 33f ? "HIGH" : "MEDIUM",
            ValueText = $"{hot.AverageTime:F2} ms",
            IndustryText = "行业水平 10ms",
            RecommendText = "推荐单函数 \u2264 15ms",
            Summary = $"函数 {hot.Name} 平均耗时 {hot.AverageTime:F2} ms，调用 {hot.Calls} 次。",
            Suggestions = new List<string>
            {
                "拆分函数逻辑，避免在 Update 中执行重计算。",
                "检查是否存在 LINQ、反射、频繁字符串拼接。",
                "对可缓存结果使用对象池或预计算。"
            }
        });
    }

    static void AddPowerItems(List<DiagnosisItem> items, List<DevicePowerConsumeInfoDto> powerList)
    {
        if (powerList.Count == 0)
        {
            return;
        }

        var peakPower = powerList.Max(item => item.BatteryPower);
        var peakTemp = powerList.Max(item => item.CpuTemperate);
        items.Add(new DiagnosisItem
        {
            Id = "power-battery",
            Category = "硬件参数",
            Title = "瞬时功耗峰值",
            Severity = peakPower > 5f ? "HIGH" : peakPower > 3f ? "MEDIUM" : "LOW",
            ValueText = $"{peakPower:F2} W",
            IndustryText = "行业水平 3W",
            RecommendText = "UWA 推荐值 \u2264 3W",
            Summary = $"瞬时功耗峰值 {peakPower:F2} W，CPU 温度峰值 {peakTemp}\u2103。",
            Suggestions = new List<string>
            {
                "降低目标帧率或启用 Adaptive Performance。",
                "减少 GPU Overdraw 与后处理开销。",
                "排查后台线程与网络轮询导致的 CPU 唤醒。"
            }
        });
    }

    static string FormatBytes(long bytes)
    {
        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        return $"{bytes / 1024.0 / 1024.0:F2} MB";
    }
}
