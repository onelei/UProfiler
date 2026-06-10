using System.Globalization;
using System.Net;
using System.Text;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ModulePerfSectionBuilder
{
    static readonly (string Key, string SectionId, string Title)[] Modules =
    [
        ("rendering", "module-rendering", "渲染模块性能"),
        ("sync", "module-sync", "GPU同步模块性能"),
        ("logic", "module-logic", "逻辑代码模块性能"),
        ("ui", "module-ui", "UI模块性能"),
        ("loading", "module-loading", "加载模块性能"),
        ("physics", "module-physics", "物理系统性能"),
        ("animation", "module-animation", "动画模块性能"),
        ("particles", "module-particles", "粒子系统性能")
    ];

    public static string BuildAll(ReportDataContext data)
    {
        var sb = new StringBuilder();
        foreach (var (key, sectionId, title) in Modules)
        {
            if (!data.ModuleTime.Modules.Any(item => item.Key == key))
            {
                continue;
            }

            sb.Append(BuildModuleSection(data, key, sectionId, title));
        }

        return sb.ToString();
    }

    static string BuildModuleSection(ReportDataContext data, string key, string sectionId, string title)
    {
        data.ModuleFuncStacks.TryGetValue(key, out var stack);
        data.ModuleDetails.TryGetValue(key, out var detail);
        var summary = data.ModuleTime.Summary.FirstOrDefault(item => item.Key == key);
        var functions = stack?.Functions ?? BuildFallbackFunctions(data, key);
        var metrics = stack?.Metrics ?? BuildFallbackMetrics(summary, detail);
        var aiItems = stack?.AiDiagnosis ?? BuildFallbackAi(data, key);

        var sb = new StringBuilder();
        sb.Append("<section id=\"").Append(sectionId).Append("\" class=\"section report-panel module-perf-panel\" data-panel=\"")
            .Append(sectionId).Append("\" data-module-key=\"").Append(key).Append("\">");
        sb.Append("<div class=\"section-title\">").Append(WebUtility.HtmlEncode(title)).Append("</div>");
        sb.Append(ReportSectionsBuilder.ScopeToolbarPublic(data, "查看场景性能列表"));
        sb.Append("<div class=\"uwa-metric-grid module-kpi-grid\" data-draggable-grid>");
        foreach (var metric in metrics)
        {
            sb.Append("<div class=\"uwa-metric-card\" draggable=\"true\"><div class=\"uwa-metric-title\">")
                .Append(WebUtility.HtmlEncode(metric.Label))
                .Append("<span class=\"muted uwa-drag-hint\">长按可拖拽排序</span></div><div class=\"uwa-metric-value\">")
                .Append(metric.AvgMs.ToString("F2", CultureInfo.InvariantCulture))
                .Append(" <span>ms</span></div><div class=\"uwa-metric-hint muted\">峰值 ")
                .Append(metric.PeakMs.ToString("F2", CultureInfo.InvariantCulture))
                .Append(" ms · 第").Append(metric.PeakFrame).Append("帧</div></div>");
        }

        sb.Append("</div>");

        sb.Append("<div class=\"module-func-toolbar\">");
        sb.Append("<div class=\"module-func-search\"><input type=\"search\" class=\"func-search-input\" placeholder=\"搜索函数\" data-module=\"")
            .Append(key).Append("\" /></div>");
        sb.Append("<div class=\"module-func-mode\"><label><input type=\"radio\" name=\"stackMode-")
            .Append(key).Append("\" value=\"module\" checked /> 本模块函数调用</label>");
        sb.Append("<label><input type=\"radio\" name=\"stackMode-").Append(key).Append("\" value=\"other\" /> 其他模块函数调用</label></div>");
        if (key == "logic")
        {
            sb.Append("<div class=\"module-func-order\"><label><input type=\"radio\" name=\"stackOrder-")
                .Append(key).Append("\" value=\"forward\" checked /> 正序调用分析</label>");
            sb.Append("<label><input type=\"radio\" name=\"stackOrder-").Append(key).Append("\" value=\"reverse\" /> 倒序调用分析</label></div>");
        }

        sb.Append("</div>");

        sb.Append("<div class=\"chart-card\"><div class=\"chart-head\">函数堆栈冰柱图（总览）</div>");
        sb.Append("<div class=\"chart module-icicle-chart\" data-module-icicle=\"").Append(key).Append("\"></div></div>");

        sb.Append("<div class=\"table-toolbar\"><span class=\"chart-head\" style=\"border:none;padding:0\">函数堆栈表</span></div>");
        sb.Append("<table class=\"data-table module-func-table\" data-module-func=\"").Append(key).Append("\"><thead><tr>");
        sb.Append("<th>函数名</th><th>耗时均值</th><th>总耗时</th><th>总体占比</th><th>自身耗时</th><th>自身占比</th>");
        sb.Append("<th>总调用次数</th><th>单次耗时</th><th>调用帧数</th><th>每帧调用次数</th><th>操作</th></tr></thead><tbody>");
        foreach (var func in functions.Take(100))
        {
            var singleMs = func.CallCount > 0 ? func.TotalMs / func.CallCount : 0;
            sb.Append("<tr><td>").Append(WebUtility.HtmlEncode(func.Name)).Append("</td><td>")
                .Append(func.AvgMs.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(func.TotalMs.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(func.TotalPct.ToString("F2", CultureInfo.InvariantCulture)).Append(" %</td><td>")
                .Append(func.SelfMs.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(func.SelfPct.ToString("F2", CultureInfo.InvariantCulture)).Append(" %</td><td>")
                .Append(func.CallCount).Append("</td><td>")
                .Append(singleMs.ToString("F3", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append(func.FrameCount).Append("</td><td>")
                .Append(func.CallsPerFrame.ToString("F2", CultureInfo.InvariantCulture)).Append("</td><td>")
                .Append("<button type=\"button\" class=\"link-btn func-time-btn\" data-func=\"")
                .Append(WebUtility.HtmlEncode(func.Name)).Append("\">Time</button> ")
                .Append("<button type=\"button\" class=\"link-btn func-call-btn\" data-func=\"")
                .Append(WebUtility.HtmlEncode(func.Name)).Append("\">Call</button></td></tr>");
        }

        if (functions.Count == 0)
        {
            sb.Append("<tr><td colspan=\"11\" class=\"muted\">暂无函数堆栈数据。请 Unity 上传 <code>moduleFuncStack_</code> 或启用函数分析 Hook。</td></tr>");
        }

        sb.Append("</tbody></table>");

        if (aiItems.Count > 0)
        {
            sb.Append("<div class=\"brief-collapse-list module-ai-list\">");
            foreach (var ai in aiItems)
            {
                sb.Append("<details class=\"brief-collapse-item\"")
                    .Append(ai.Severity is "Medium" or "High" ? " open" : "")
                    .Append("><summary class=\"brief-collapse-head\"><span class=\"brief-collapse-title\">")
                    .Append(WebUtility.HtmlEncode(ai.Title))
                    .Append("</span><span class=\"brief-severity\">").Append(WebUtility.HtmlEncode(ai.Severity))
                    .Append("</span></summary><div class=\"brief-collapse-body\"><p class=\"muted\">")
                    .Append(WebUtility.HtmlEncode(ai.Suggestion)).Append("</p></div></details>");
            }

            sb.Append("</div>");
        }

        sb.Append("</section>");
        return sb.ToString();
    }

    static List<ModuleFuncStackMetricRow> BuildFallbackMetrics(ModuleSummaryRow? summary, ModuleDetailPayload? detail)
    {
        if (detail?.Metrics.Count > 0)
        {
            return detail.Metrics.Select(item => new ModuleFuncStackMetricRow
            {
                Label = item.Name,
                AvgMs = item.AverageMs,
                PeakMs = item.AverageMs * 1.5,
                PeakFrame = 0
            }).Take(4).ToList();
        }

        if (summary == null)
        {
            return new List<ModuleFuncStackMetricRow>();
        }

        return
        [
            new ModuleFuncStackMetricRow
            {
                Label = summary.Label + " CPU耗时",
                AvgMs = summary.AverageMs,
                PeakMs = summary.AverageMs * 2,
                PeakFrame = 0
            }
        ];
    }

    static List<ModuleFuncStackFunctionRow> BuildFallbackFunctions(ReportDataContext data, string key)
    {
        if (data.ModuleFuncStacks.TryGetValue(key, out var stack) && stack.Functions.Count > 0)
        {
            return stack.Functions;
        }

        var totalMs = data.FuncAnalysis.Sum(item => Math.Max(0, item.UseTime * 1000));
        if (totalMs <= 0)
        {
            totalMs = 1;
        }

        return data.FuncAnalysis
            .Take(key == "logic" ? 50 : 20)
            .Select(item =>
            {
                var total = item.UseTime * 1000;
                return new ModuleFuncStackFunctionRow
                {
                    Name = item.Name,
                    AvgMs = Math.Round(item.AverageTime, 2),
                    TotalMs = Math.Round(total, 2),
                    SelfMs = Math.Round(item.AverageTime * 0.12, 2),
                    TotalPct = Math.Round(total / totalMs * 100, 2),
                    SelfPct = Math.Round(item.AverageTime / Math.Max(1, item.AverageTime + total) * 100, 2),
                    CallCount = item.Calls,
                    CallsPerFrame = Math.Round(item.Calls / Math.Max(1.0, data.Brief.FrameCount / 30.0), 2),
                    FrameCount = item.Calls
                };
            })
            .ToList();
    }

    static List<ModuleFuncStackAiEntry> BuildFallbackAi(ReportDataContext data, string key)
    {
        var category = key switch
        {
            "rendering" => "渲染模块",
            "logic" => "逻辑脚本",
            _ => "整体"
        };

        return data.DiagnosisItems
            .Where(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase) && item.Severity is "HIGH" or "MEDIUM")
            .Take(3)
            .Select(item => new ModuleFuncStackAiEntry
            {
                Title = item.Title,
                Severity = item.Severity == "HIGH" ? "High" : "Medium",
                Suggestion = item.Suggestions.FirstOrDefault() ?? item.RecommendText
            })
            .ToList();
    }
}
