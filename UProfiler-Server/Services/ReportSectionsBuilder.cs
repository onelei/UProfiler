using System.Globalization;
using System.Net;
using System.Text;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ReportSectionsBuilder
{
    public static string BuildBriefSection(ReportDataContext data)
    {
        var sb = new StringBuilder();
        sb.Append("<section id=\"brief\" class=\"section report-panel\" data-panel=\"brief\">");
        sb.Append("<div class=\"section-title\">性能简报</div>");
        sb.Append("<div class=\"brief-meta\">");
        sb.Append("<p><span class=\"brief-label\">报告名称：</span>")
            .Append(WebUtility.HtmlEncode(data.DeviceInfo?.DeviceModel ?? "本地报告"))
            .Append("_").Append(WebUtility.HtmlEncode(data.SessionKey)).Append("_overview</p>");
        sb.Append("<p><span class=\"brief-label\">测试时间：</span>")
            .Append(WebUtility.HtmlEncode(data.TestInfo?.TestTime ?? "-")).Append("</p>");
        sb.Append("<p><span class=\"brief-label\">测试帧数：</span>")
            .Append(data.Brief.FrameCount.ToString(CultureInfo.InvariantCulture)).Append("</p>");
        sb.Append("</div>");

        sb.Append("<div class=\"uwa-section-head\"><h3>数据汇总</h3>");
        sb.Append("<button type=\"button\" class=\"link-btn muted\" disabled title=\"需 UWA 云端同档次数据\">同档次排名</button></div>");

        sb.Append("<div class=\"brief-kpi-grid\">");
        foreach (var kpi in data.Brief.Kpis)
        {
            sb.Append("<div class=\"brief-kpi-card\"><div class=\"brief-kpi-head\">")
                .Append(WebUtility.HtmlEncode(kpi.Label))
                .Append("</div><div class=\"brief-kpi-value\">")
                .Append(WebUtility.HtmlEncode(kpi.Value))
                .Append(" <span>").Append(WebUtility.HtmlEncode(kpi.Unit)).Append("</span></div>");
            if (kpi.TaskCount > 0)
            {
                sb.Append("<div class=\"brief-kpi-task\">优化任务队列 ").Append(kpi.TaskCount).Append("</div>");
            }

            sb.Append("</div>");
        }

        sb.Append("</div>");

        sb.Append("<div class=\"brief-detail-head\"><h3>FPS均值详情</h3><p class=\"muted\">")
            .Append(WebUtility.HtmlEncode(data.Brief.SummaryText))
            .Append("，可优化项/检测项：")
            .Append(data.Brief.OptimizableCount).Append("/").Append(data.Brief.TotalMetricCount)
            .Append("</p>");
        sb.Append("<label class=\"brief-filter\"><input type=\"checkbox\" id=\"briefOptimOnly\" /> 仅显示优化项</label></div>");

        sb.Append("<table class=\"data-table brief-metrics-table\" id=\"briefMetricsTable\"><thead><tr>");
        sb.Append("<th>B类指标名称</th><th>行业水平</th><th>指标数值</th><th>历史趋势</th><th>优化项</th></tr></thead><tbody>");
        foreach (var metric in data.Brief.Metrics)
        {
            var rowClass = metric.TaskCount > 0 ? "brief-metric-row optimizable" : "brief-metric-row";
            sb.Append("<tr class=\"").Append(rowClass).Append("\"><td>")
                .Append(WebUtility.HtmlEncode(metric.Label)).Append("</td><td>-</td><td>")
                .Append(WebUtility.HtmlEncode(metric.Value)).Append(" ")
                .Append(WebUtility.HtmlEncode(metric.Unit)).Append("</td><td>-</td><td>")
                .Append(metric.TaskCount).Append("</td></tr>");
        }

        sb.Append("</tbody></table></section>");
        return sb.ToString();
    }

    public static string BuildBasicInfoSection(ReportDataContext data)
    {
        var maxFrame = MaxFrame(data);
        var maxFrameMs = data.MaxFps > 0 ? Math.Round(1000.0 / data.MaxFps, 2) : 0;
        var jankPct = data.Brief.FrameCount > 0
            ? Math.Round(data.Jank.JankCount * 100.0 / data.Brief.FrameCount, 2)
            : 0;
        var over40Pct = EstimateOver40MsRatio(data);

        var sb = new StringBuilder();
        sb.Append("<section id=\"basicinfo\" class=\"section report-panel\" data-panel=\"basicinfo\">");
        sb.Append("<div class=\"section-title\">运行信息</div>");
        sb.Append(ScopeToolbar(data, "查看场景性能列表"));
        sb.Append("<div class=\"uwa-metric-grid\">");
        sb.Append(MetricCard("FPS", data.AvgFps.ToString("F2", CultureInfo.InvariantCulture), "帧/秒",
            $"当前帧：{data.MinFps} 帧/秒 · 最大值：{data.MaxFps} 帧/秒", "basicFpsChart", "fps"));
        sb.Append(MetricCard("CPU每帧耗时", over40Pct.ToString("F2", CultureInfo.InvariantCulture), "%",
            $">40ms帧数占比 · 最大值：{maxFrameMs} ms (第{FindMaxFrameMsIndex(data)}帧)", "basicFrameTimeChart", "frametime"));
        sb.Append(MetricCard("Jank均值", data.Jank.JankPerMinute.ToString("F2", CultureInfo.InvariantCulture), "次/分钟",
            $"卡顿率：{jankPct} % · Big Jank：{data.Jank.BigJankCount}", "basicJankChart", "frametime"));
        sb.Append(MetricCard("PSS内存", data.PeakPssMb > 0 ? data.PeakPssMb.ToString("F2", CultureInfo.InvariantCulture) : FormatBytes(data.PeakTotalAllocated), data.PeakPssMb > 0 ? "MB" : "",
            $"峰值 · Reserved Mono：{FormatBytes(data.PeakMonoUsed)}", "basicPssChart", data.PeakPssMb > 0 ? "pss" : "memory"));
        if (HasPowerData(data))
        {
            sb.Append(MetricCard("功率", data.PeakBatteryPower.ToString("F0", CultureInfo.InvariantCulture), "mW",
                $"峰值 (第{PeakPowerFrame(data)}帧)", "basicPowerChart", "power"));
            sb.Append(MetricCard("综合温度", data.PeakCpuTemp.ToString(CultureInfo.InvariantCulture), "℃",
                $"峰值 · 变化量 {TempDelta(data)} ℃", "basicTempChart", "temperature"));
        }

        sb.Append("</div>");
        sb.Append("<div class=\"info-grid\" style=\"margin-top:20px\">");
        sb.Append("<div class=\"info-card\"><h3>测试信息</h3><table>");
        AppendInfoRow(sb, "产品名", data.TestInfo?.ProductName);
        AppendInfoRow(sb, "包名", data.TestInfo?.PackageName);
        AppendInfoRow(sb, "平台", data.TestInfo?.Platform);
        AppendInfoRow(sb, "版本号", data.TestInfo?.Version);
        AppendInfoRow(sb, "测试时长", data.TestInfo?.TestTime);
        AppendInfoRow(sb, "总览帧范围", maxFrame > 0 ? $"0-{maxFrame}帧" : "-");
        sb.Append("</table></div>");
        sb.Append("<div class=\"info-card\"><h3>设备信息</h3><table>");
        AppendInfoRow(sb, "Unity 版本", data.DeviceInfo?.UnityVersion);
        AppendInfoRow(sb, "操作系统", data.DeviceInfo?.OperatingSystem);
        AppendInfoRow(sb, "设备型号", data.DeviceInfo?.DeviceModel);
        AppendInfoRow(sb, "处理器", data.DeviceInfo?.ProcessorType);
        AppendInfoRow(sb, "显卡", data.DeviceInfo?.GraphicsDeviceName);
        AppendInfoRow(sb, "分辨率", data.DeviceInfo == null ? null : $"{data.DeviceInfo.ScreenWidth} x {data.DeviceInfo.ScreenHeight}");
        AppendInfoRow(sb, "系统内存", data.DeviceInfo?.SystemMemorySize.ToString(CultureInfo.InvariantCulture) + " MB");
        sb.Append("</table></div></div></section>");
        return sb.ToString();
    }

    public static string BuildSceneOverviewSection(ReportDataContext data)
    {
        var sceneCount = data.SceneManagement.Scenes.Count;
        return $"""
<section id="scene-overview" class="section report-panel" data-panel="scene-overview">
<div class="section-title">场景概览 · 性能概览</div>
<div class="panel-toolbar"><span class="muted">场景性能概览 · 共 {sceneCount} 个场景分段</span>
<button type="button" class="link-btn" disabled>导出数据</button></div>
<div class="chart-card">
<div class="chart-head">场景性能概览 · CPU耗时(ms)</div>
<div id="sceneCpuBarChart" class="chart scene-cpu-bar-chart" data-chart="scene-cpu-bar"></div>
</div>
<div class="uwa-table-wrap">
<div class="table-toolbar"><span class="chart-head" style="border:none;padding:0">场景性能概览列表</span></div>
<table class="data-table uwa-scene-table uwa-scene-overview-table"><thead><tr>
<th>场景名</th><th>帧数</th><th>begin</th><th>end</th><th>FPS均值(帧/秒)</th>
<th>PSS内存峰值(MB)</th><th>Reserved Mono峰值(MB)</th><th>CPU耗时均值(ms)</th>
<th>CPU耗时峰值(ms)</th><th>Triangles峰值(个)</th><th>DrawCall峰值(个)</th>
</tr></thead><tbody>
{BuildSceneOverviewTableRows(data.SceneManagement.Scenes)}
</tbody></table>
</div>
</section>
""";
    }

    public static string BuildSceneManagementSection(ReportDataContext data)
    {
        var hint = data.SceneManagement.HasSceneInfo
            ? "<p class=\"panel-hint muted\">游戏运行过程中小于100帧的场景已隐藏。Unity 上传 <code>sceneInfo_</code> 后可显示真实场景分段。</p>"
            : "<p class=\"panel-hint muted\">未检测到 <code>sceneInfo_</code> 上传，以下为整段测试的合并视图。Unity 上传场景切换数据后可显示真实场景名与分段（格式见 todo.md）。</p>";

        return $"""
<section id="scene-management" class="section report-panel" data-panel="scene-management">
<div class="section-title">场景概览 · 场景管理</div>
{hint}
<div class="chart-card scene-chart-card">
<div class="chart-head">每帧耗时曲线 <span class="muted chart-unit">单位: ms</span></div>
<div id="sceneFrameTimeChart" class="chart scene-frametime-chart" data-chart="frametime"></div>
<div id="sceneFrameHint" class="chart-frame-hint muted"></div>
</div>
<div class="uwa-table-wrap">
<table class="data-table uwa-scene-table"><thead><tr>
<th>场景</th><th>起始帧</th><th>结束帧</th><th>总帧数</th><th>平均每帧耗时(ms)</th><th>查看</th>
</tr></thead><tbody>
{BuildSceneTableRowsWithAction(data.SceneManagement.Scenes)}
</tbody></table>
</div>
</section>
""";
    }

    public static string BuildGpuSections(ReportDataContext data)
    {
        var hasRender = data.RenderInfos?.RenderInfoList.Count > 0;
        var gpuPressure = data.Brief.Metrics.FirstOrDefault(item => item.Label == "GPU压力系数")?.Value ?? "-";
        var renderBlock = hasRender
            ? $"""
<div class="uwa-metric-grid">
{MetricCard("GPU Clocks", gpuPressure, "%", $"DrawCall 峰值 {data.PeakDrawCall} · 三角面峰值 {data.PeakTriangles}", "gpuClocksChart", "render-dc")}
</div>
<div class="chart-legend muted">GPU Bound：当 GPU 压力过高时帧率可能受 GPU 算力限制。真实 GPU Clocks 需上传 <code>gpuBandwidth_</code>（见 todo.md）。</div>
<div class="chart-grid">
<div class="chart-card"><div class="chart-head">DrawCall 趋势</div><div id="gpuDrawChart" class="chart" data-chart="render-dc"></div></div>
<div class="chart-card"><div class="chart-head">三角面趋势</div><div id="gpuTriChart" class="chart" data-chart="render-tri"></div></div>
</div>
<div class="uwa-related-metrics"><div class="chart-head">相关指标</div>
<div class="cards">
<div class="card kpi"><div class="label">DrawCall 峰值</div><div class="value">{data.PeakDrawCall}</div></div>
<div class="card kpi"><div class="label">三角面峰值</div><div class="value">{data.PeakTriangles}</div></div>
<div class="card kpi"><div class="label">SetPass 峰值</div><div class="value">{(data.RenderInfos?.RenderInfoList.Count > 0 ? data.RenderInfos.RenderInfoList.Max(item => item.SetPassCall) : 0)}</div></div>
</div></div>
"""
            : "<p class=\"muted\">暂无渲染数据。请在 Unity 中启用 <code>enableRenderInfo</code>。</p>";

        return $"""
<section id="gpu-render" class="section report-panel" data-panel="gpu-render">
<div class="section-title">GPU分析 · GPU 渲染分析</div>
{ScopeToolbar(data, "查看场景性能列表")}
{renderBlock}
</section>
<section id="gpu-bandwidth" class="section report-panel" data-panel="gpu-bandwidth">
<div class="section-title">GPU分析 · GPU 带宽分析</div>
{ScopeToolbar(data, "查看场景性能列表")}
<p class="muted">GPU Total Bandwidth 需 Unity 上传 <code>gpuBandwidth_</code>。当前基于 DrawCall / 三角面估算渲染压力。</p>
<div class="chart-card"><div class="chart-head">GPU Total Bandwidth（估算）</div>
<div id="gpuBandwidthChart" class="chart" data-chart="gpu-bandwidth"></div></div>
</section>
<section id="gpu-summary" class="section report-panel" data-panel="gpu-summary">
<div class="section-title">GPU分析 · 指标汇总</div>
{ScopeToolbar(data, "查看场景性能列表")}
<div class="uwa-related-metrics"><div class="chart-head">主要指标</div>
<div class="cards">
<div class="card kpi"><div class="label">GPU 压力系数均值</div><div class="value">{gpuPressure}<span class="unit">%</span></div></div>
<div class="card kpi"><div class="label">DrawCall 峰值</div><div class="value">{data.PeakDrawCall}</div></div>
<div class="card kpi"><div class="label">三角面峰值</div><div class="value">{data.PeakTriangles}</div></div>
</div></div>
<div class="uwa-related-metrics"><div class="chart-head">次要指标</div>
<div class="cards">
<div class="card kpi"><div class="label">SetPass 峰值</div><div class="value">{(data.RenderInfos?.RenderInfoList.Count > 0 ? data.RenderInfos.RenderInfoList.Max(item => item.SetPassCall) : 0)}</div></div>
<div class="card kpi"><div class="label">顶点峰值</div><div class="value">{(data.RenderInfos?.RenderInfoList.Count > 0 ? data.RenderInfos.RenderInfoList.Max(item => item.Vertices) : 0)}</div></div>
</div></div>
</section>
""";
    }

    public static string BuildThreadStackSection(ReportDataContext data)
    {
        var sb = new StringBuilder();
        sb.Append("<section id=\"thread-stack\" class=\"section report-panel\" data-panel=\"thread-stack\">");
        sb.Append("<div class=\"section-title\">各线程 CPU 调用堆栈</div>");
        sb.Append("<p class=\"muted\">线程总览：MainThread · Render Thread · Loading.AsyncRead · Audio Stream Thread（完整多线程堆栈需 <code>threadStack_</code>）</p>");
        sb.Append("<div class=\"chart-card\"><div class=\"chart-head\">各线程 CPU 耗时</div>");
        sb.Append("<div id=\"threadStackChart\" class=\"chart\" data-chart=\"thread-stack\"></div></div>");

        if (data.FuncAnalysis.Count == 0)
        {
            sb.Append("<p class=\"muted\">暂无线程堆栈数据。当前可查看函数性能近似替代。</p>");
        }
        else
        {
            sb.Append("<div class=\"table-toolbar\"><span class=\"chart-head\" style=\"border:none;padding:0\">MainThread 热点函数（近似）</span></div>");
            sb.Append("<table class=\"data-table\"><thead><tr><th>函数</th><th>平均耗时(ms)</th><th>调用次数</th><th>总耗时(s)</th></tr></thead><tbody>");
            foreach (var func in data.FuncAnalysis.Take(30))
            {
                sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(func.Name)).Append("</td><td>")
                    .Append(func.AverageTime.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                    .Append(func.Calls).Append("</td><td>")
                    .Append(func.UseTime.ToString("F3", CultureInfo.InvariantCulture)).Append("</td></tr>");
            }

            sb.Append("</tbody></table>");
        }

        sb.Append("</section>");
        return sb.ToString();
    }

    public static string BuildJankSections(ReportDataContext data)
    {
        var j = data.Jank;
        var total = Math.Max(1, j.JankCount);
        var sb = new StringBuilder();
        sb.Append("<section id=\"jank-frames\" class=\"section report-panel\" data-panel=\"jank-frames\">");
        sb.Append("<div class=\"section-title\">卡顿分析 · 卡顿点分析</div>");
        sb.Append(ScopeToolbar(data, "", "卡顿帧总览"));
        sb.Append("<div class=\"jank-summary-grid\">");
        sb.Append(JankSummaryCard("全部卡顿帧", j.JankCount, 100));
        sb.Append(JankSummaryCard("严重卡顿帧", j.SevereJankCount, Math.Round(j.SevereJankCount * 100.0 / total, 2)));
        sb.Append(JankSummaryCard("GC.Collect类卡顿帧", 0, 0));
        sb.Append(JankSummaryCard("加载类卡顿帧", j.LoadingJankCount, Math.Round(j.LoadingJankCount * 100.0 / total, 2)));
        sb.Append(JankSummaryCard("其他类卡顿帧", j.OtherJankCount, Math.Round(j.OtherJankCount * 100.0 / total, 2)));
        sb.Append("</div>");
        sb.Append("<div class=\"chart-card\"><div class=\"chart-head\">卡顿帧分布（帧耗时）</div>");
        sb.Append("<div id=\"jankFrameChart\" class=\"chart\" data-chart=\"frametime\"></div></div>");

        if (j.JankHotFunctions.Count > 0)
        {
            sb.Append("<div class=\"table-toolbar\"><span class=\"chart-head\" style=\"border:none;padding:0\">卡顿帧函数列表</span></div>");
            sb.Append("<table class=\"data-table\"><thead><tr>");
            sb.Append("<th>函数名称</th><th>重点卡顿点数量</th><th>总耗时占比</th><th>自身耗时占比</th>");
            sb.Append("<th>总耗时</th><th>自身耗时</th><th>分布卡顿点数量</th><th>操作</th></tr></thead><tbody>");
            foreach (var func in j.JankHotFunctions)
            {
                sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(func.Name)).Append("</td><td>")
                    .Append(func.KeyJankCount).Append("</td><td>")
                    .Append(func.TotalRatio.ToString("F2", CultureInfo.InvariantCulture)).Append(" %</td><td>")
                    .Append(func.SelfRatio.ToString("F2", CultureInfo.InvariantCulture)).Append(" %</td><td>")
                    .Append(func.TotalMs.ToString("F3", CultureInfo.InvariantCulture)).Append(" ms</td><td>")
                    .Append(func.SelfMs.ToString("F3", CultureInfo.InvariantCulture)).Append(" ms</td><td>")
                    .Append(func.SpreadJankCount).Append("</td><td>")
                    .Append("<button type=\"button\" class=\"link-btn\" disabled>函数详情</button></td></tr>");
            }

            sb.Append("</tbody></table>");
        }
        else if (j.Frames.Count > 0)
        {
            sb.Append("<table class=\"data-table\"><thead><tr><th>帧号</th><th>FPS</th><th>帧耗时(ms)</th><th>类型</th></tr></thead><tbody>");
            foreach (var frame in j.Frames.Take(100))
            {
                sb.Append("<tr><td>").Append(frame.FrameIndex).Append("</td><td>")
                    .Append(frame.Fps).Append("</td><td>")
                    .Append(frame.FrameMs.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                    .Append(WebUtility.HtmlEncode(frame.JankType)).Append("</td></tr>");
            }

            sb.Append("</tbody></table>");
        }
        else
        {
            sb.Append("<p class=\"muted\">未检测到明显卡顿帧。</p>");
        }

        sb.Append("</section>");

        sb.Append("<section id=\"jank-func\" class=\"section report-panel\" data-panel=\"jank-func\">");
        sb.Append("<div class=\"section-title\">卡顿分析 · 重点函数分析</div>");
        sb.Append(ScopeToolbar(data, "查看场景性能列表"));
        sb.Append("<div class=\"jank-func-tabs\">");
        sb.Append("<span class=\"jank-tab\">GC.Collect卡顿点</span><span class=\"jank-tab\">加载卡顿点</span>");
        sb.Append("<span class=\"jank-tab muted\">动画卡顿点</span><span class=\"jank-tab muted\">物理卡顿点</span></div>");
        if (j.HotFunctions.Count == 0)
        {
            sb.Append("<p class=\"muted\">暂无重点函数数据。请启用函数分析 Hook。</p>");
        }
        else
        {
            sb.Append("<table class=\"data-table\"><thead><tr><th>函数</th><th>平均耗时(ms)</th><th>调用次数</th><th>总耗时(s)</th></tr></thead><tbody>");
            foreach (var func in j.HotFunctions)
            {
                sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(func.Name)).Append("</td><td>")
                    .Append(func.AverageMs.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                    .Append(func.Calls).Append("</td><td>")
                    .Append(func.TotalSeconds.ToString("F3", CultureInfo.InvariantCulture)).Append("</td></tr>");
            }

            sb.Append("</tbody></table>");
        }

        sb.Append("</section>");
        return sb.ToString();
    }

    public static string BuildMemorySections(ReportDataContext data)
    {
        var peakPss = data.PeakPssMb > 0 ? $"{data.PeakPssMb:F2} MB" : "-";
        var peakMono = FormatBytes(data.PeakMonoUsed);
        var peakTotal = FormatBytes(data.PeakTotalAllocated);

        var sb = new StringBuilder();
        sb.Append("""
<section id="memory-occupy" class="section report-panel" data-panel="memory-occupy">
<div class="section-title">内存分析 · 内存占用</div>
""");
        sb.Append("<div class=\"mem-peak-summary muted\">");
        sb.Append("PSS内存占用峰值： ").Append(WebUtility.HtmlEncode(peakPss));
        sb.Append(" · Reserved Total峰值： ").Append(WebUtility.HtmlEncode(peakTotal));
        sb.Append(" · Reserved Mono峰值： ").Append(WebUtility.HtmlEncode(peakMono));
        sb.Append("</div>");
        sb.Append("""
<div class="chart-grid">
<div class="chart-card"><div class="chart-head">PSS内存占用 / Unity 内存</div><div id="memOccupyChart" class="chart" data-chart="memory"></div></div>
<div class="chart-card"><div class="chart-head">PSS 内存（Android）</div><div id="memPssChart" class="chart" data-chart="pss"></div></div>
</div>
<div class="table-toolbar"><span class="chart-head" style="border:none;padding:0">当前帧内存堆栈（采样首帧）</span></div>
<table class="data-table"><thead><tr><th>内存堆栈</th><th>内存占用</th><th>推荐值</th><th>总体占比</th><th>操作</th></tr></thead><tbody>
""");
        sb.Append(BuildMemoryStackRows(data));
        sb.Append("</tbody></table></section>");

        sb.Append("<section id=\"memory-resource\" class=\"section report-panel\" data-panel=\"memory-resource\">");
        sb.Append("<div class=\"section-title\">内存分析 · 资源内存</div>");
        if (data.ResourceSummary.Count == 0)
        {
            sb.Append("<p class=\"muted\">暂无资源内存分布。请启用 <code>enableResMemoryDistributionInfo</code>。</p>");
        }
        else
        {
            sb.Append("<div class=\"chart-card\"><div class=\"chart-head\">资源内存占用趋势（各类型均值）</div>");
            sb.Append("<div id=\"memResourceTrendChart\" class=\"chart\" data-chart=\"resource-trend\"></div></div>");
            sb.Append("<div class=\"module-summary\" style=\"margin-top:16px\">");
            sb.Append("<div class=\"module-summary-card\"><div class=\"chart-head\">资源类型占比预览</div>");
            sb.Append("<div id=\"memResourceChart\" class=\"chart module-pie\" data-chart=\"resource-pie\"></div></div>");
            sb.Append("<div class=\"module-summary-card\"><div class=\"chart-head\">资源对象快照</div>");
            sb.Append("<p class=\"muted\" style=\"padding:12px 16px\">在场景切换或手动 Dump 时触发资源对象快照。单击资源类型查看具体资源（需 Unity 上传详细列表）。</p></div></div>");
            sb.Append("<table class=\"data-table\"><thead><tr><th>资源类型</th><th>数量</th><th>内存占用</th><th>推荐值</th><th>操作</th></tr></thead><tbody>");
            foreach (var row in data.ResourceSummary.Where(item => item.AvgSizeBytes > 0 || item.AvgCount > 0))
            {
                sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(row.Label)).Append("</td><td>")
                    .Append(row.AvgCount).Append("</td><td>")
                    .Append(FormatBytes(row.AvgSizeBytes)).Append("</td><td>")
                    .Append(WebUtility.HtmlEncode(row.RecommendText)).Append("</td><td>")
                    .Append("<button type=\"button\" class=\"link-btn\" disabled>查看具体资源使用</button></td></tr>");
            }

            sb.Append("</tbody></table>");
            sb.Append("<div class=\"chart-card\" style=\"margin-top:16px\"><div class=\"chart-head\">资源内存占比</div>");
            sb.Append("<div id=\"memResourcePieChart\" class=\"chart\" data-chart=\"resource-pie\"></div></div>");
        }

        sb.Append("</section>");

        sb.Append("""
<section id="memory-lua" class="section report-panel" data-panel="memory-lua">
<div class="section-title">内存分析 · Lua内存</div>
""");
        sb.Append(ScopeToolbar(data));
        sb.Append("<p class=\"muted\">LUA堆内存 / Table数量 / Function数量 需 Unity 上传 <code>luaMemory_</code>（见 todo.md）。</p>");
        sb.Append("<div class=\"chart-card\"><div class=\"chart-head\">LUA堆内存（占位）</div>");
        sb.Append("<div class=\"chart-empty\">暂无 Lua 内存采样数据</div></div></section>");

        sb.Append("<section id=\"memory-mono\" class=\"section report-panel\" data-panel=\"memory-mono\">");
        sb.Append("<div class=\"section-title\">内存分析 · Mono内存</div>");
        sb.Append(ScopeToolbar(data));
        sb.Append("<div class=\"uwa-metric-grid\">");
        sb.Append(MetricCard("Reserved Mono峰值", peakMono, "",
            $"当前帧：{peakMono}", "memMonoChart", "memory-mono"));
        sb.Append("</div>");
        sb.Append("<div class=\"chart-card\"><div class=\"chart-head\">Mono Reserved / Mono Used</div>");
        sb.Append("<div id=\"memMonoDetailChart\" class=\"chart\" data-chart=\"memory-mono\"></div></div>");
        sb.Append("</section>");
        return sb.ToString();
    }

    public static string BuildBatterySection(ReportDataContext data)
    {
        if (!HasPowerData(data))
        {
            return """
<section id="battery" class="section report-panel" data-panel="battery">
<div class="section-title">耗电量</div>
<p class="muted">暂无功耗数据。Android 真机请启用 enableMobileConsumptionInfo。</p>
</section>
""";
        }

        var avgPower = data.PowerInfos!.DevicePowerConsumeInfos.Average(item => item.BatteryPower);
        return $"""
<section id="battery" class="section report-panel" data-panel="battery">
<div class="section-title">耗电量</div>
{ScopeToolbar(data, "查看场景性能列表")}
<div class="uwa-metric-grid">
{MetricCard("电量", avgPower.ToString("F2", CultureInfo.InvariantCulture), "%", $"每万帧耗电均值 · 峰值 {data.PeakBatteryPower:F0} mW", "batteryChart", "power")}
</div>
<div class="chart-card"><div class="chart-head">瞬时功耗趋势</div>
<div id="batteryTrendChart" class="chart" data-chart="power"></div></div>
</section>
""";
    }

    public static string BuildTemperatureSection(ReportDataContext data)
    {
        if (!HasPowerData(data))
        {
            return """
<section id="temperature" class="section report-panel" data-panel="temperature">
<div class="section-title">温度变化量</div>
<p class="muted">暂无温度数据。Android 真机请启用 enableMobileConsumptionInfo。</p>
</section>
""";
        }

        return $"""
<section id="temperature" class="section report-panel" data-panel="temperature">
<div class="section-title">温度变化量</div>
{ScopeToolbar(data, "查看场景性能列表")}
<div class="uwa-metric-grid">
{MetricCard("综合温度", data.PeakCpuTemp.ToString(CultureInfo.InvariantCulture), "℃", $"变化量 {TempDelta(data)} ℃", "temperatureChart", "temperature")}
</div>
<div class="chart-card"><div class="chart-head">CPU 温度趋势</div>
<div id="temperatureTrendChart" class="chart" data-chart="temperature"></div></div>
</section>
""";
    }

    public static string BuildCustomSections()
    {
        return """
<section id="custom-dashboard" class="section report-panel" data-panel="custom-dashboard">
<div class="section-title">自定义模块 · 自定义面板</div>
<div class="custom-panel-list">
<span class="custom-panel-chip">发热降频分析</span>
<span class="custom-panel-chip">GPU压力导致的发热耗电分析</span>
<span class="custom-panel-chip">UGUI的CPU端开销整合</span>
<button type="button" class="link-btn" disabled>新建面板</button>
</div>
<p class="muted">自定义面板指标曲线需 Unity 上传 <code>customDashboard_</code>（见 todo.md）。下方为运行信息指标预览。</p>
<div class="uwa-metric-grid">
<div class="uwa-metric-card"><div class="uwa-metric-title">FPS</div><div class="chart chart-sm" data-chart="fps"></div></div>
<div class="uwa-metric-card"><div class="uwa-metric-title">CPU频率</div><div class="chart-empty chart-sm">需设备采样</div></div>
</div>
</section>
<section id="custom-funcs" class="section report-panel" data-panel="custom-funcs">
<div class="section-title">自定义模块 · 自定义函数组</div>
<p class="muted">需 Unity 上传 <code>apiFuncs_</code> 数据（见 todo.md）。</p>
</section>
<section id="custom-vars" class="section report-panel" data-panel="custom-vars">
<div class="section-title">自定义模块 · 自定义变量</div>
<p class="muted">需 Unity 上传 <code>apiInfo_</code> 数据（见 todo.md）。</p>
</section>
<section id="custom-code" class="section report-panel" data-panel="custom-code">
<div class="section-title">自定义模块 · 自定义代码段</div>
<p class="muted">需 Unity 上传 <code>apiCodeFrame_</code> 数据（见 todo.md）。</p>
</section>
""";
    }

    public static string BuildResourceManagementSections(ReportDataContext data)
    {
        var sb = new StringBuilder();
        sb.Append("<section id=\"resource-summary\" class=\"section report-panel\" data-panel=\"resource-summary\">");
        sb.Append("<div class=\"section-title\">资源管理 · 资源管理汇总</div>");
        sb.Append("<div class=\"cards\">");
        sb.Append("<div class=\"card kpi\"><div class=\"label\">Resources.Load千帧调用次数</div><div class=\"value muted\">-</div></div>");
        sb.Append("<div class=\"card kpi\"><div class=\"label\">AssetBundle.Load千帧调用次数</div><div class=\"value muted\">-</div></div>");
        sb.Append("<div class=\"card kpi\"><div class=\"label\">Instantiate千帧调用次数</div><div class=\"value muted\">-</div></div>");
        sb.Append("</div>");
        sb.Append("<p class=\"muted\">AB/资源加载事件 TOP 10 需 Unity 上传 <code>resourceManagement_</code>。下方为资源内存类型汇总。</p>");

        sb.Append("<div class=\"resource-top-grid\">");
        sb.Append(ResourceTopTable("AssetBundle 加载次数 TOP 10", new[] { "AB路径", "加载方式", "加载次数" }, EmptyResourceRow()));
        sb.Append(ResourceTopTable("资源加载次数 TOP 10", new[] { "资源路径", "加载方式", "加载次数" }, EmptyResourceRow()));
        sb.Append(ResourceTopTable("Instantiate 调用次数 TOP 10", new[] { "资源名称", "调用次数" }, EmptyResourceRow()));
        sb.Append("</div>");

        if (data.ResourceSummary.Count > 0)
        {
            sb.Append("<div class=\"table-toolbar\" style=\"margin-top:20px\"><span class=\"chart-head\" style=\"border:none;padding:0\">资源内存类型汇总</span></div>");
            sb.Append("<table class=\"data-table\"><thead><tr><th>类型</th><th>数量</th><th>平均占用</th><th>峰值</th></tr></thead><tbody>");
            foreach (var row in data.ResourceSummary)
            {
                sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(row.Label)).Append("</td><td>")
                    .Append(row.AvgCount).Append("</td><td>")
                    .Append(FormatBytes(row.AvgSizeBytes)).Append("</td><td>")
                    .Append(FormatBytes(row.PeakSizeBytes)).Append("</td></tr>");
            }

            sb.Append("</tbody></table>");
        }

        sb.Append("</section>");

        var placeholder = "<p class=\"muted\">该细分项需 Unity 上传 <code>resourceManagement_</code> 事件流（见 todo.md）。</p>";
        sb.Append("<section id=\"resource-ab\" class=\"section report-panel\" data-panel=\"resource-ab\">");
        sb.Append("<div class=\"section-title\">资源管理 · AssetBundle 加载&amp;卸载</div>").Append(placeholder).Append("</section>");
        sb.Append("<section id=\"resource-load\" class=\"section report-panel\" data-panel=\"resource-load\">");
        sb.Append("<div class=\"section-title\">资源管理 · 资源加载&amp;卸载</div>").Append(placeholder).Append("</section>");
        sb.Append("<section id=\"resource-instantiate\" class=\"section report-panel\" data-panel=\"resource-instantiate\">");
        sb.Append("<div class=\"section-title\">资源管理 · 资源实例化&amp;激活</div>").Append(placeholder).Append("</section>");
        return sb.ToString();
    }

    public static string BuildLogSection(ReportDataContext data, string logJson)
    {
        if (data.LogLines.Count == 0)
        {
            return "<section id=\"log\" class=\"section report-panel\" data-panel=\"log\"><div class=\"section-title\">运行日志</div><p class=\"muted\">暂无日志数据。可在 GOT 中启用 Log 监控。</p></section>";
        }

        var counts = CountLogLevels(data.LogLines);
        return $"""
<section id="log" class="section report-panel" data-panel="log">
<div class="section-title">运行日志</div>
<div class="log-filter-bar">
<button type="button" class="log-filter-btn active" data-log-filter="all">All ({counts.All})</button>
<button type="button" class="log-filter-btn" data-log-filter="log">Log ({counts.Log})</button>
<button type="button" class="log-filter-btn" data-log-filter="warning">Warning ({counts.Warning})</button>
<button type="button" class="log-filter-btn" data-log-filter="error">Error ({counts.Error})</button>
<button type="button" class="log-filter-btn" data-log-filter="exception">Exception ({counts.Exception})</button>
<button type="button" class="link-btn" id="logExport" disabled>导出Log</button>
</div>
<div class="table-toolbar"><span class="muted">帧数 · 场景 · Log内容 · 共 {data.LogLines.Count} 条</span>
<button id="logMore" type="button" class="link-btn">加载更多</button></div>
<div id="logBox" class="log-box log-table"></div>
<script type="application/json" id="logData">{logJson}</script>
</section>
""";
    }

    static string BuildSceneOverviewTableRows(IReadOnlyList<SceneTableRow> scenes)
    {
        if (scenes.Count == 0)
        {
            return "<tr><td colspan=\"11\" class=\"muted\">暂无场景数据</td></tr>";
        }

        var sb = new StringBuilder();
        foreach (var row in scenes)
        {
            sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(row.SceneName)).Append("</td><td>")
                .Append(row.FrameCount).Append("</td><td>")
                .Append(row.StartFrame).Append("</td><td>")
                .Append(row.EndFrame).Append("</td><td>")
                .Append(row.AvgFps.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(row.PeakPssMb.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(row.PeakMonoMb.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(row.AvgFrameMs.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(row.PeakCpuMs.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(row.PeakTriangles).Append("</td><td>")
                .Append(row.PeakDrawCall).Append("</td></tr>");
        }

        return sb.ToString();
    }

    static string BuildSceneTableRowsWithAction(IReadOnlyList<SceneTableRow> scenes)
    {
        if (scenes.Count == 0)
        {
            return "<tr><td colspan=\"6\" class=\"muted\">暂无场景数据</td></tr>";
        }

        var sb = new StringBuilder();
        foreach (var row in scenes)
        {
            sb.Append("<tr class=\"scene-row\" data-start=\"").Append(row.StartFrame).Append("\" data-end=\"")
                .Append(row.EndFrame).Append("\" data-name=\"").Append(WebUtility.HtmlEncode(row.SceneName)).Append("\"><td>")
                .Append(WebUtility.HtmlEncode(row.SceneName)).Append("</td><td>")
                .Append(row.StartFrame).Append("</td><td>")
                .Append(row.EndFrame).Append("</td><td>")
                .Append(row.FrameCount).Append("</td><td>")
                .Append(row.AvgFrameMs.ToString("F2", CultureInfo.InvariantCulture))
                .Append("</td><td><button type=\"button\" class=\"link-btn scene-detail-btn\">场景详情</button></td></tr>");
        }

        return sb.ToString();
    }

    static string BuildMemoryStackRows(ReportDataContext data)
    {
        var monitor = data.UProfilerInfos?.GetAll().FirstOrDefault();
        var pss = data.MemoryUseDatas?.MemoryUsedList.FirstOrDefault();
        var total = monitor?.TotalAllocatedMemory ?? 0;
        var mono = monitor?.MonoUsedSize ?? 0;
        var pssVal = pss?.PssMemorySize ?? data.PeakPssMb;
        var rows = new (string Name, long Bytes, string Recommend)[]
        {
            ("PSS内存占用", (long)(pssVal * 1024 * 1024), "2000 MB"),
            ("Reserved Total", total, "1150 MB"),
            ("Reserved Mono", mono, "-"),
            ("其它", Math.Max(0, total - mono), "-")
        };

        var sum = rows.Sum(item => item.Bytes);
        if (sum <= 0) sum = 1;

        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            var ratio = row.Name is "PSS内存占用" or "其它" ? "-" : $"{row.Bytes * 100.0 / sum:F2} %";
            sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(row.Name)).Append("</td><td>")
                .Append(FormatBytes(row.Bytes)).Append("</td><td>")
                .Append(WebUtility.HtmlEncode(row.Recommend)).Append("</td><td>")
                .Append(ratio).Append("</td><td>")
                .Append("<button type=\"button\" class=\"link-btn\" disabled>查看具体资源使用</button></td></tr>");
        }

        return sb.ToString();
    }

    static string ScopeToolbar(ReportDataContext data, string linkText = "", string prefix = "总览")
    {
        var maxFrame = MaxFrame(data);
        var sb = new StringBuilder();
        sb.Append("<div class=\"scope-toolbar\"><span class=\"scope-label\">")
            .Append(WebUtility.HtmlEncode(prefix)).Append("（0-").Append(maxFrame).Append("帧）</span>");
        sb.Append("<span class=\"scope-chip muted\">指定帧</span><span class=\"scope-chip muted\">指定场景</span>");
        if (!string.IsNullOrWhiteSpace(linkText))
        {
            sb.Append("<a href=\"#scene-overview\" class=\"link-btn\">").Append(WebUtility.HtmlEncode(linkText)).Append("</a>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    static string MetricCard(string title, string value, string unit, string hint, string chartId, string chartType)
    {
        return $"""
<div class="uwa-metric-card">
<div class="uwa-metric-title">{WebUtility.HtmlEncode(title)}<span class="muted uwa-drag-hint">长按可拖拽排序</span></div>
<div class="uwa-metric-value">{WebUtility.HtmlEncode(value)} <span>{WebUtility.HtmlEncode(unit)}</span></div>
<div class="uwa-metric-hint muted">{WebUtility.HtmlEncode(hint)}</div>
<div id="{chartId}" class="chart chart-sm" data-chart="{chartType}"></div>
</div>
""";
    }

    static string JankSummaryCard(string label, int count, double percent)
    {
        return $"""
<div class="jank-summary-card"><div class="label">{WebUtility.HtmlEncode(label)}</div>
<div class="value">{count} <span class="muted">（{percent.ToString("F2", CultureInfo.InvariantCulture)} %）</span></div></div>
""";
    }

    static string ResourceTopTable(string title, string[] headers, string bodyRow)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"resource-top-card\"><div class=\"chart-head\">").Append(WebUtility.HtmlEncode(title)).Append("</div>");
        sb.Append("<table class=\"data-table\"><thead><tr>");
        foreach (var header in headers)
        {
            sb.Append("<th>").Append(WebUtility.HtmlEncode(header)).Append("</th>");
        }

        sb.Append("</tr></thead><tbody>").Append(bodyRow).Append("</tbody></table></div>");
        return sb.ToString();
    }

    static string EmptyResourceRow()
        => "<tr><td colspan=\"3\" class=\"muted\">在测试过程中未发生/未检测到该数据</td></tr>";

    static void AppendInfoRow(StringBuilder sb, string label, string? value)
    {
        sb.Append("<tr><th>").Append(WebUtility.HtmlEncode(label)).Append("</th><td>");
        sb.Append(WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "-" : value));
        sb.Append("</td></tr>");
    }

    static (int All, int Log, int Warning, int Error, int Exception) CountLogLevels(List<string> lines)
    {
        var log = 0;
        var warning = 0;
        var error = 0;
        var exception = 0;
        foreach (var line in lines)
        {
            if (line.Contains("[Exception]", StringComparison.OrdinalIgnoreCase)) exception++;
            else if (line.Contains("[Error]", StringComparison.OrdinalIgnoreCase)) error++;
            else if (line.Contains("[Warning]", StringComparison.OrdinalIgnoreCase)) warning++;
            else if (line.Contains("[Log]", StringComparison.OrdinalIgnoreCase)) log++;
        }

        return (lines.Count, log, warning, error, exception);
    }

    static bool HasPowerData(ReportDataContext data)
        => data.PowerInfos?.DevicePowerConsumeInfos.Count > 0;

    static int MaxFrame(ReportDataContext data)
        => data.FrameRates?.FrameRateList.Count > 0
            ? data.FrameRates.FrameRateList.Max(item => item.FrameIndex)
            : 0;

    static int TempDelta(ReportDataContext data)
    {
        if (data.PowerInfos?.DevicePowerConsumeInfos.Count <= 1)
        {
            return 0;
        }

        var powerList = data.PowerInfos!.DevicePowerConsumeInfos;
        var temps = powerList.Select(item => item.CpuTemperate).ToList();
        return temps.Max() - temps.Min();
    }

    static int PeakPowerFrame(ReportDataContext data)
    {
        if (data.PowerInfos?.DevicePowerConsumeInfos.Count == 0)
        {
            return 0;
        }

        return data.PowerInfos!.DevicePowerConsumeInfos
            .OrderByDescending(item => item.BatteryPower)
            .First()
            .FrameIndex;
    }

    static int FindMaxFrameMsIndex(ReportDataContext data)
    {
        var fpsList = data.FrameRates?.FrameRateList ?? new List<UProfilerFrameInfoDto>();
        if (fpsList.Count == 0)
        {
            return 0;
        }

        return fpsList.OrderByDescending(item => item.Frame > 0 ? 1000.0 / item.Frame : 0).First().FrameIndex;
    }

    static double EstimateOver40MsRatio(ReportDataContext data)
    {
        var fpsList = data.FrameRates?.FrameRateList ?? new List<UProfilerFrameInfoDto>();
        if (fpsList.Count == 0)
        {
            return 0;
        }

        var over = fpsList.Count(item => item.Frame > 0 && 1000.0 / item.Frame > 40);
        return Math.Round(over * 100.0 / fpsList.Count, 2);
    }

    static string FormatBytes(long bytes)
    {
        if (bytes <= 0)
        {
            return "0 B";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024.0:F1} KB";
        }

        return $"{bytes / 1024.0 / 1024.0:F2} MB";
    }
}
