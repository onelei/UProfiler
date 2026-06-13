using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ReportHtmlBuilder
{
    static readonly JsonSerializerOptions JsonCamelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static readonly JsonSerializerOptions JsonSafeHtml = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static string Build(ReportDataContext data)
    {
        var chartPayload = BuildChartPayload(data);
        var modulePayload = BuildModulePayload(data);
        var capturePayload = BuildCapturePayload(data);
        var diagnosisJson = JsonSerializer.Serialize(data.DiagnosisItems, JsonCamelCase);
        var funcJson = BuildFuncJson(data);
        var logJson = JsonSerializer.Serialize(data.LogLines, JsonSafeHtml);
        var productName = data.TestInfo?.ProductName ?? "UProfiler 性能报告";
        var reportTitle = $"{productName} - 性能分析报告";
        var packageName = data.TestInfo?.PackageName ?? data.PackageName;
        var hasPower = HasPowerData(data);
        var hasPss = HasPssData(data);

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\" />");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\" />");
        sb.Append("<title>").Append(WebUtility.HtmlEncode(reportTitle)).Append("</title>");
        sb.Append("<link rel=\"stylesheet\" href=\"").Append(StaticAssets.Css("portal.css")).Append("\" />");
        sb.Append("<link rel=\"stylesheet\" href=\"").Append(StaticAssets.Css("account.css")).Append("\" />");
        sb.Append("<link rel=\"stylesheet\" href=\"").Append(StaticAssets.Css("report.css")).Append("\" />");
        sb.Append("</head><body>");

        sb.Append(PortalHtmlBuilder.BuildTopNav(
            PortalHtmlBuilder.NavTab.Report,
            string.IsNullOrWhiteSpace(packageName) ? null : packageName));
        sb.Append("<div class=\"layout\">");
        sb.Append(ReportSidebarBuilder.Build(data));
        sb.Append("<main class=\"main\">");
        sb.Append(BuildReportToolbar(data, productName));
        sb.Append(ReportSectionsBuilder.BuildBriefSection(data));
        sb.Append(ReportSectionsBuilder.BuildBasicInfoSection(data));
        sb.Append(ReportSectionsBuilder.BuildSceneOverviewSection(data));
        sb.Append(ReportSectionsBuilder.BuildSceneManagementSection(data));
        sb.Append(ReportSectionsBuilder.BuildGpuSections(data));
        sb.Append(BuildTrendSection(data, chartPayload, hasPower, hasPss));
        sb.Append(BuildModuleTimeSection(data));
        sb.Append(ReportSectionsBuilder.BuildThreadStackSection(data));
        sb.Append(BuildDiagnosisSection());
        sb.Append(ReportSectionsBuilder.BuildJankSections(data));
        sb.Append(ReportSectionsBuilder.BuildMemorySections(data));
        sb.Append(ReportSectionsBuilder.BuildBatterySection(data));
        sb.Append(ReportSectionsBuilder.BuildTemperatureSection(data));
        sb.Append(ReportSectionsBuilder.BuildCustomSections(data));
        sb.Append(ModulePerfSectionBuilder.BuildAll(data));
        sb.Append(ReportSectionsBuilder.BuildResourceManagementSections(data));
        sb.Append(BuildFuncSection(data, funcJson));
        sb.Append(ReportSectionsBuilder.BuildLogSection(data, logJson));
        sb.Append("<footer class=\"footer\"><a href=\"/\">返回首页</a>");
        if (!string.IsNullOrWhiteSpace(packageName))
        {
            var pkgUrl = ProjectCatalog.EncodePackage(packageName);
            sb.Append(" · <a href=\"/project/").Append(pkgUrl).Append("\">项目</a>");
            sb.Append(" · <a href=\"/project/").Append(pkgUrl).Append("/performance\">报告列表</a>");
        }
        sb.Append("</footer>");
        sb.Append("</main></div>");

        sb.Append("<script>window.chartPayload=").Append(chartPayload).Append(";</script>");
        sb.Append("<script>window.modulePayload=").Append(modulePayload).Append(";</script>");
        sb.Append("<script>window.moduleDetails=").Append(BuildModuleDetailsPayload(data)).Append(";</script>");
        sb.Append("<script>window.capturePayload=").Append(capturePayload).Append(";</script>");
        sb.Append("<script>window.reportSession=").Append(JsonSerializer.Serialize(data.SessionKey, JsonSafeHtml)).Append(";</script>");
        sb.Append("<script>window.diagnosisItems=").Append(diagnosisJson).Append(";</script>");
        sb.Append("<script>window.scenePayload=").Append(JsonSerializer.Serialize(data.SceneManagement, JsonCamelCase)).Append(";</script>");
        sb.Append("<script>window.briefPayload=").Append(JsonSerializer.Serialize(data.Brief, JsonCamelCase)).Append(";</script>");
        sb.Append("<script>window.threadStackPayload=").Append(JsonSerializer.Serialize(data.ThreadStack, JsonCamelCase)).Append(";</script>");
        sb.Append("<script>window.gpuBandwidthPayload=").Append(JsonSerializer.Serialize(data.GpuBandwidth, JsonCamelCase)).Append(";</script>");
        sb.Append("<script>window.luaMemoryPayload=").Append(JsonSerializer.Serialize(data.LuaMemory, JsonCamelCase)).Append(";</script>");
        sb.Append("<script>window.customDashboardPayload=").Append(JsonSerializer.Serialize(data.CustomDashboard, JsonCamelCase)).Append(";</script>");
        sb.Append("<script>window.resourceSummary=").Append(JsonSerializer.Serialize(data.ResourceSummary, JsonCamelCase)).Append(";</script>");
        sb.Append("<script>window.moduleFuncStacks=").Append(JsonSerializer.Serialize(data.ModuleFuncStacks, JsonCamelCase)).Append(";</script>");
        sb.Append("<script defer src=\"").Append(StaticAssets.Js("report.js")).Append("\"></script>");
        sb.Append("<script defer src=\"").Append(StaticAssets.Js("account.js")).Append("\"></script>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    static string BuildReportToolbar(ReportDataContext data, string productName)
    {
        var hw = data.HardwareInfo;
        var fpsBadge = hw?.TargetFrameRate > 0
            ? $"<span class=\"toolbar-badge\" title=\"目标帧率\">{hw.TargetFrameRate} FPS</span>"
            : "";
        var netBadge = !string.IsNullOrWhiteSpace(hw?.NetworkType)
            ? $"<span class=\"toolbar-badge\" title=\"网络制式\">{WebUtility.HtmlEncode(hw.NetworkType)}</span>"
            : "";

        return $"""
<div class="report-toolbar">
  <nav class="breadcrumb">
    <span>{WebUtility.HtmlEncode(productName)}</span>
    <span>/</span>
    <span>{WebUtility.HtmlEncode(data.TestInfo?.Platform ?? "-")}</span>
    <span>/</span>
    <span id="breadcrumbPanel">性能简报</span>
  </nav>
  <div class="report-toolbar-meta">
    {fpsBadge}{netBadge}
    <span>{WebUtility.HtmlEncode(data.DeviceInfo?.DeviceModel ?? "-")}</span>
    <span>采样 {data.TestInfo?.IntervalFrame.ToString(CultureInfo.InvariantCulture) ?? "-"} 帧</span>
    <button type="button" class="link-btn" id="reportDownloadJson" title="下载本报告全部结构化数据">下载 JSON</button>
  </div>
</div>
""";
    }

    static string BuildTrendSection(ReportDataContext data, string chartPayload, bool hasPower, bool hasPss)
    {
        var sb = new StringBuilder();
        sb.Append("<section id=\"trend\" class=\"section report-panel\" data-panel=\"trend\">");
        sb.Append("<div class=\"section-title\">总体性能趋势</div>");
        sb.Append(BuildDownsampleNote(chartPayload));
        sb.Append("<div class=\"chart-grid\">");
        sb.Append(BuildChartCard("fpsChart", "fps", "帧率报告"));
        sb.Append(BuildChartCard("memoryChart", "memory", "内存占用"));
        sb.Append(BuildChartCard("renderChart", "render", "渲染报告"));
        if (hasPower)
        {
            sb.Append(BuildChartCard("powerChart", "power", "温度与功耗"));
        }
        if (hasPss)
        {
            sb.Append(BuildChartCard("pssChart", "pss", "PSS 内存"));
        }
        sb.Append("</div></section>");
        return sb.ToString();
    }

    static string BuildDiagnosisSection()
    {
        return """
<section id="diagnosis" class="section report-panel" data-panel="diagnosis">
<div class="section-title">性能诊断</div>
<div class="diag-toolbar">
<button class="filter-btn active" data-filter="ALL">ALL</button>
<button class="filter-btn low" data-filter="LOW">LOW</button>
<button class="filter-btn medium" data-filter="MEDIUM">MEDIUM</button>
<button class="filter-btn high" data-filter="HIGH">HIGH</button>
</div><div class="diag-layout">
<div id="diagList" class="diag-list"></div>
<div id="diagDetail" class="diag-detail"></div>
</div></section>
""";
    }

    static string BuildChartCard(string id, string chartType, string title)
    {
        return $"""
<div class="chart-card"><div class="chart-head">{WebUtility.HtmlEncode(title)}</div><div id="{id}" class="chart" data-chart="{chartType}"><div class="chart-loading">滚动到此处加载图表…</div></div></div>
""";
    }

    static string BuildDownsampleNote(string chartPayloadJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(chartPayloadJson);
            if (!doc.RootElement.TryGetProperty("meta", out var meta))
            {
                return "";
            }

            var parts = new List<string>();
            foreach (var prop in meta.EnumerateObject())
            {
                if (prop.Name.EndsWith("Original", StringComparison.Ordinal) && prop.Value.GetInt32() > ChartDataDownsampler.DefaultMaxPoints)
                {
                    var key = prop.Name.Replace("Original", "", StringComparison.Ordinal);
                    if (meta.TryGetProperty(key + "Shown", out var shown))
                    {
                        parts.Add($"{key} {prop.Value.GetInt32()} → {shown.GetInt32()} 点");
                    }
                }
            }

            if (parts.Count == 0)
            {
                return "";
            }

            return $"<p class=\"downsample-note\">图表已降采样以提升加载性能：{string.Join("；", parts)}</p>";
        }
        catch
        {
            return "";
        }
    }

    static bool HasPowerData(ReportDataContext data)
        => data.PowerInfos?.DevicePowerConsumeInfos.Count > 0;

    static bool HasPssData(ReportDataContext data)
        => data.MemoryUseDatas?.MemoryUsedList.Count > 0;

    static string BuildChartPayload(ReportDataContext data)
    {
        var fps = data.FrameRates?.FrameRateList ?? new List<UProfilerFrameInfoDto>();
        var monitor = data.UProfilerInfos?.GetAll() ?? new List<UProfilerInfoDto>();
        var render = data.RenderInfos?.RenderInfoList ?? new List<RenderInfoDto>();
        var pss = data.MemoryUseDatas?.MemoryUsedList ?? new List<MemoryUseDataDto>();
        var power = data.PowerInfos?.DevicePowerConsumeInfos ?? new List<DevicePowerConsumeInfoDto>();

        var fpsX = fps.Select(item => item.FrameIndex).ToArray();
        var fpsY = fps.Select(item => item.Frame).ToArray();
        DownsamplePair(ref fpsX, ref fpsY, out var fpsOriginal, out var fpsShown);

        var memX = monitor.Select(item => item.FrameIndex).ToArray();
        var mono = monitor.Select(item => Math.Round(item.MonoUsedSize / 1024.0 / 1024.0, 2)).ToArray();
        var total = monitor.Select(item => Math.Round(item.TotalAllocatedMemory / 1024.0 / 1024.0, 2)).ToArray();
        var reserved = monitor.Select(item => Math.Round(item.UnityTotalReservedMemory / 1024.0 / 1024.0, 2)).ToArray();
        DownsamplePair(ref memX, ref mono, ref total, ref reserved, out var memOriginal, out var memShown);

        var renderX = render.Select(item => item.FrameIndex).ToArray();
        var setPass = render.Select(item => item.SetPassCall).ToArray();
        var drawCall = render.Select(item => item.DrawCall).ToArray();
        var vertices = render.Select(item => item.Vertices).ToArray();
        var triangles = render.Select(item => item.Triangles).ToArray();
        DownsampleRender(ref renderX, ref setPass, ref drawCall, ref vertices, ref triangles, out var renderOriginal, out var renderShown);

        var pssX = pss.Select(item => item.FrameIndex).ToArray();
        var pssY = pss.Select(item => Math.Round(item.PssMemorySize, 2)).ToArray();
        DownsamplePair(ref pssX, ref pssY, out var pssOriginal, out var pssShown);

        var powerX = power.Select(item => item.FrameIndex).ToArray();
        var batteryPower = power.Select(item => Math.Round(item.BatteryPower, 2)).ToArray();
        var cpuTemp = power.Select(item => item.CpuTemperate).ToArray();
        DownsamplePower(ref powerX, ref batteryPower, ref cpuTemp, out var powerOriginal, out var powerShown);

        var hardware = data.HardwareInfo?.Samples ?? new List<HardwareSampleDto>();
        var hwX = hardware.Select(item => item.FrameIndex).ToArray();
        var cpuFreq = hardware.Select(item => Math.Round(item.CpuFreqMHz, 2)).ToArray();
        var netRecv = hardware.Select(item => Math.Round(item.NetRecvKB, 2)).ToArray();
        var netSent = hardware.Select(item => Math.Round(item.NetSentKB, 2)).ToArray();
        var lowMemoryFrames = hardware.Where(item => item.LowMemory).Select(item => item.FrameIndex).ToArray();

        var payload = new
        {
            meta = new
            {
                fpsOriginal,
                fpsShown,
                memOriginal,
                memShown,
                renderOriginal,
                renderShown
            },
            fps = new { x = fpsX, y = fpsY },
            frametime = new
            {
                x = fpsX,
                y = fpsY.Select(f => f > 0 ? Math.Round(1000.0 / f, 2) : 0).ToArray()
            },
            memory = new { x = memX, monoUsed = mono, totalAllocated = total, unityReserved = reserved },
            render = new { x = renderX, setPass, drawCall, vertices, triangles },
            pss = new { x = pssX, y = pssY },
            power = new { x = powerX, batteryPower, cpuTemp },
            hardware = new { x = hwX, cpuFreq, netRecv, netSent },
            lowMemoryFrames
        };

        return JsonSerializer.Serialize(payload, JsonCamelCase);
    }

    static string BuildModulePayload(ReportDataContext data)
    {
        return JsonSerializer.Serialize(data.ModuleTime, JsonCamelCase);
    }

    static string BuildCapturePayload(ReportDataContext data)
    {
        var payload = new
        {
            sessionKey = data.SessionKey,
            productName = data.TestInfo?.ProductName ?? "",
            platform = data.TestInfo?.Platform ?? "",
            version = data.TestInfo?.Version ?? "",
            deviceModel = data.DeviceInfo?.DeviceModel ?? "",
            frames = data.CaptureFrames.FrameImages.Keys.OrderBy(item => item).ToArray(),
            hasCaptures = data.CaptureFrames.FrameImages.Count > 0
        };
        return JsonSerializer.Serialize(payload, JsonCamelCase);
    }

    static string BuildModuleTimeSection(ReportDataContext data)
    {
        if (data.ModuleTime.X.Count == 0)
        {
            return """
<section id="module-time" class="section report-panel" data-panel="module-time"><div class="section-title">模块耗时统计</div>
<p class="muted">暂无帧率采样数据，无法生成模块耗时统计。</p></section>
""";
        }

        var summaryRows = new StringBuilder();
        foreach (var row in data.ModuleTime.Summary)
        {
            var valueClass = row.OverRecommend ? "module-over" : "";
            summaryRows.Append("<tr class=\"module-row-clickable\" data-module=\"")
                .Append(WebUtility.HtmlEncode(row.Key))
                .Append("\"><td><span class=\"module-dot\" style=\"background:")
                .Append(row.Color)
                .Append("\"></span>")
                .Append(WebUtility.HtmlEncode(row.Label))
                .Append("</td><td")
                .Append(string.IsNullOrEmpty(valueClass) ? "" : " class=\"" + valueClass + "\"")
                .Append(">")
                .Append(row.AverageMs.ToString("F2", CultureInfo.InvariantCulture))
                .Append(" ms</td><td>")
                .Append(row.RecommendMs.ToString("F1", CultureInfo.InvariantCulture))
                .Append(" ms</td></tr>");
        }

        return $"""
<section id="module-time" class="section report-panel" data-panel="module-time">
<div class="section-title">模块耗时统计</div>
<p class="muted module-note">点击饼图或表格行进入模块详情；点击图表采样点可在左侧查看对应截图。</p>
<div class="module-layout">
<div id="capturePanel" class="capture-panel hidden">
  <div class="capture-head"><span id="captureScene">-</span><button id="captureExpand" type="button" title="放大">⤢</button></div>
  <div class="capture-body"><img id="captureImage" alt="采样截图" /><div id="capturePlaceholder" class="capture-placeholder">暂无截图</div></div>
  <div class="capture-nav">
    <button id="capturePrev" type="button" class="capture-nav-btn" title="上一采样" aria-label="上一采样">‹</button>
    <span id="captureNavInfo">- / -</span>
    <button id="captureNext" type="button" class="capture-nav-btn" title="下一采样" aria-label="下一采样">›</button>
  </div>
  <div class="capture-foot"><span id="captureFrameLabel">第 - 帧</span><span id="captureDevice">-</span></div>
</div>
<div class="module-main">
  <div id="moduleOverview" class="module-view">
  <div class="module-summary">
    <div class="module-summary-card">
      <div class="chart-head">模块占比预览</div>
      <div id="modulePieChart" class="chart module-pie"></div>
    </div>
    <div class="module-summary-card">
      <div class="chart-head">模块均值与推荐值</div>
      <table class="module-table"><thead><tr><th>模块分类</th><th>CPU 耗时均值</th><th>推荐值</th></tr></thead>
      <tbody>{summaryRows}</tbody></table>
    </div>
  </div>
  <div class="chart-card module-chart-card">
    <div class="chart-head">各模块 CPU 耗时</div>
    <div id="moduleTimeChart" class="chart module-time-chart" data-chart="module"></div>
  </div>
  </div>
  <div id="moduleDetail" class="module-view hidden">
    <nav class="module-breadcrumb">
      <a href="#module-time" id="moduleBackLink">各模块 CPU 耗时</a>
      <span class="module-breadcrumb-sep">/</span>
      <span id="moduleDetailCrumb">模块详情</span>
    </nav>
    <p id="moduleDetailHint" class="muted module-detail-hint hidden"></p>
    <div class="module-detail-summary">
      <div class="module-summary-card">
        <div class="chart-head" id="moduleDetailPieTitle">函数占比预览</div>
        <div id="moduleDetailPie" class="chart module-pie"></div>
      </div>
      <div class="module-summary-card module-detail-table-wrap">
        <div class="chart-head" id="moduleDetailTableTitle">指标列表</div>
        <table class="module-table module-detail-table">
          <thead id="moduleDetailTableHead"><tr><th>名称</th><th>均值</th><th>占比</th><th>操作</th></tr></thead>
          <tbody id="moduleDetailTableBody"></tbody>
        </table>
      </div>
    </div>
    <div class="chart-card module-chart-card">
      <div class="chart-head" id="moduleDetailChartTitle">模块 CPU 耗时</div>
      <div id="moduleDetailChart" class="chart module-time-chart"></div>
    </div>
  </div>
</div>
</div>
</section>
<div id="captureModal" class="capture-modal hidden"><div class="capture-modal-inner"><button id="captureModalClose" type="button">×</button><img id="captureModalImage" alt="采样截图" /></div></div>
""";
    }

    static void DownsamplePair(ref int[] x, ref int[] y, out int original, out int shown)
    {
        original = x.Length;
        if (x.Length <= ChartDataDownsampler.DefaultMaxPoints)
        {
            shown = original;
            return;
        }

        var indices = ChartDataDownsampler.PickIndices(x.Length);
        x = ChartDataDownsampler.DownsampleByIndices(x, indices);
        y = ChartDataDownsampler.DownsampleByIndices(y, indices);
        shown = x.Length;
    }

    static void DownsamplePair(ref int[] x, ref double[] a, ref double[] b, ref double[] c, out int original, out int shown)
    {
        original = x.Length;
        if (x.Length <= ChartDataDownsampler.DefaultMaxPoints)
        {
            shown = original;
            return;
        }

        var indices = ChartDataDownsampler.PickIndices(x.Length);
        x = ChartDataDownsampler.DownsampleByIndices(x, indices);
        a = ChartDataDownsampler.DownsampleByIndices(a, indices);
        b = ChartDataDownsampler.DownsampleByIndices(b, indices);
        c = ChartDataDownsampler.DownsampleByIndices(c, indices);
        shown = x.Length;
    }

    static void DownsamplePair(ref int[] x, ref double[] y, out int original, out int shown)
    {
        original = x.Length;
        if (x.Length <= ChartDataDownsampler.DefaultMaxPoints)
        {
            shown = original;
            return;
        }

        var indices = ChartDataDownsampler.PickIndices(x.Length);
        x = ChartDataDownsampler.DownsampleByIndices(x, indices);
        y = ChartDataDownsampler.DownsampleByIndices(y, indices);
        shown = x.Length;
    }

    static void DownsampleRender(ref int[] x, ref long[] a, ref long[] b, ref long[] c, ref long[] d, out int original, out int shown)
    {
        original = x.Length;
        if (x.Length <= ChartDataDownsampler.DefaultMaxPoints)
        {
            shown = original;
            return;
        }

        var indices = ChartDataDownsampler.PickIndices(x.Length);
        x = ChartDataDownsampler.DownsampleByIndices(x, indices);
        a = ChartDataDownsampler.DownsampleByIndices(a, indices);
        b = ChartDataDownsampler.DownsampleByIndices(b, indices);
        c = ChartDataDownsampler.DownsampleByIndices(c, indices);
        d = ChartDataDownsampler.DownsampleByIndices(d, indices);
        shown = x.Length;
    }

    static void DownsamplePower(ref int[] x, ref double[] a, ref int[] b, out int original, out int shown)
    {
        original = x.Length;
        if (x.Length <= ChartDataDownsampler.DefaultMaxPoints)
        {
            shown = original;
            return;
        }

        var indices = ChartDataDownsampler.PickIndices(x.Length);
        x = ChartDataDownsampler.DownsampleByIndices(x, indices);
        a = ChartDataDownsampler.DownsampleByIndices(a, indices);
        b = ChartDataDownsampler.DownsampleByIndices(b, indices);
        shown = x.Length;
    }

    static string BuildFuncJson(ReportDataContext data)
    {
        var rows = data.FuncAnalysis.Select(item => new
        {
            name = WebUtility.HtmlEncode(item.Name),
            calls = item.Calls,
            avgTime = item.AverageTime.ToString("F2", CultureInfo.InvariantCulture),
            useTime = item.UseTime.ToString("F3", CultureInfo.InvariantCulture),
            avgMem = item.AverageMemory.ToString("F2", CultureInfo.InvariantCulture),
            severity = item.AverageTime >= 15 ? "HIGH" : item.AverageTime >= 8 ? "MEDIUM" : "LOW"
        });
        return JsonSerializer.Serialize(rows, JsonSafeHtml);
    }

    static string BuildModuleDetailsPayload(ReportDataContext data)
    {
        return JsonSerializer.Serialize(data.ModuleDetails, JsonCamelCase);
    }

    static string BuildFuncSection(ReportDataContext data, string funcJson)
    {
        if (data.FuncAnalysis.Count == 0)
        {
            return "<section id=\"func\" class=\"section report-panel\" data-panel=\"func\"><div class=\"section-title\">函数性能分析</div><p class=\"muted\">暂无函数性能数据。请在 Unity 菜单执行 Hook 注入并启用函数分析。</p></section>";
        }

        return $"""
<section id="func" class="section report-panel" data-panel="func">
<div class="section-title">函数性能分析</div>
<div class="table-toolbar"><span class="muted">按平均耗时降序 · 分页加载减少 DOM 占用</span>
<div class="pager"><button id="funcPrev" type="button">上一页</button><span id="funcPageInfo"></span><button id="funcNext" type="button">下一页</button></div></div>
<table class="data-table"><thead><tr><th>函数名</th><th>调用次数</th><th>平均耗时(ms)</th><th>总耗时(s)</th><th>平均内存(KB)</th><th>状态</th></tr></thead>
<tbody id="funcTbody"></tbody></table>
<script type="application/json" id="funcData">{funcJson}</script>
</section>
""";
    }
}
