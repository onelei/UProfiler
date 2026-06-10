using System.Net;
using System.Text;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class ReportSidebarBuilder
{
    public sealed class NavBadgeCounts
    {
        public int Trend { get; init; }
        public int Jank { get; init; }
        public int Memory { get; init; }
        public int Battery { get; init; }
        public int Temperature { get; init; }
        public int Rendering { get; init; }
        public int Loading { get; init; }
        public int Physics { get; init; }
    }

    public static NavBadgeCounts ComputeBadges(ReportDataContext data)
    {
        var trend = data.DiagnosisItems.Count(item => item.Severity is "HIGH" or "MEDIUM");
        var memory = data.DiagnosisItems.Count(item => item.Category == "内存");
        var battery = data.PowerInfos?.DevicePowerConsumeInfos.Count > 0
            ? data.DiagnosisItems.Count(item => item.Category == "硬件参数")
            : 0;
        var temp = data.PowerInfos?.DevicePowerConsumeInfos.Count > 1 ? 1 : 0;

        var summaryMap = data.ModuleTime.Summary.ToDictionary(item => item.Key, item => item);
        summaryMap.TryGetValue("rendering", out var rendering);
        summaryMap.TryGetValue("loading", out var loading);
        summaryMap.TryGetValue("physics", out var physics);

        return new NavBadgeCounts
        {
            Trend = trend,
            Jank = data.Jank.JankCount > 0 ? Math.Min(data.Jank.JankCount, 99) : 0,
            Memory = memory,
            Battery = battery,
            Temperature = temp,
            Rendering = rendering?.OverRecommend == true ? 1 : 0,
            Loading = loading?.OverRecommend == true ? 1 : 0,
            Physics = physics?.OverRecommend == true ? Math.Min(2, trend) : 0
        };
    }

    public static string Build(ReportDataContext data)
    {
        var badges = ComputeBadges(data);
        var sb = new StringBuilder();
        sb.Append("""
<aside class="sidebar uwa-sidebar">
  <nav class="sidebar-menu">
""");

        AppendLink(sb, "#brief", "性能简报", 0);
        AppendLink(sb, "#basicinfo", "运行信息", 0);

        AppendGroupStart(sb, "scene", "场景概览");
        AppendLink(sb, "#scene-overview", "性能概览", 0, sub: true);
        AppendLink(sb, "#scene-management", "场景管理", 0, sub: true);
        AppendGroupEnd(sb);

        AppendGroupStart(sb, "gpu", "GPU分析");
        AppendLink(sb, "#gpu-render", "GPU 渲染分析", 0, sub: true);
        AppendLink(sb, "#gpu-bandwidth", "GPU 带宽分析", 0, sub: true);
        AppendLink(sb, "#gpu-summary", "指标汇总", 0, sub: true);
        AppendGroupEnd(sb);

        AppendGroupStart(sb, "trend", "总体性能趋势", badges.Trend);
        AppendLink(sb, "#module-time", "模块耗时统计", 0, sub: true, moduleNav: "overview");
        AppendLink(sb, "#thread-stack", "各线程CPU调用堆栈", 0, sub: true);
        AppendModuleLinks(sb, data, badges);
        AppendGroupEnd(sb);

        AppendGroupStart(sb, "jank", "卡顿分析", badges.Jank);
        AppendLink(sb, "#jank-frames", "卡顿点分析", 0, sub: true);
        AppendLink(sb, "#jank-func", "重点函数分析", data.Jank.HotFunctions.Count, sub: true);
        AppendGroupEnd(sb);

        AppendGroupStart(sb, "memory", "内存分析", badges.Memory);
        AppendLink(sb, "#memory-occupy", "内存占用", 0, sub: true);
        AppendLink(sb, "#memory-resource", "资源内存", data.ResourceSummary.Count, sub: true);
        AppendLink(sb, "#memory-lua", "Lua内存", 0, sub: true);
        AppendLink(sb, "#memory-mono", "Mono内存", 0, sub: true);
        AppendGroupEnd(sb);

        AppendLink(sb, "#battery", "耗电量", badges.Battery);
        AppendLink(sb, "#temperature", "温度变化量", badges.Temperature);

        AppendGroupStart(sb, "custom", "自定义模块");
        AppendLink(sb, "#custom-dashboard", "自定义面板", 0, sub: true);
        AppendLink(sb, "#custom-funcs", "自定义函数组", 0, sub: true);
        AppendLink(sb, "#custom-vars", "自定义变量", 0, sub: true);
        AppendLink(sb, "#custom-code", "自定义代码段", 0, sub: true);
        AppendGroupEnd(sb);

        AppendGroupStart(sb, "resource", "资源管理");
        AppendLink(sb, "#resource-summary", "资源管理汇总", 0, sub: true);
        AppendLink(sb, "#resource-ab", "AssetBundle加载&卸载", 0, sub: true);
        AppendLink(sb, "#resource-load", "资源加载&卸载", 0, sub: true);
        AppendLink(sb, "#resource-instantiate", "资源实例化&激活", 0, sub: true);
        AppendGroupEnd(sb);

        AppendLink(sb, "#log", "运行日志", data.LogLines.Count > 0 ? 0 : 0);

        sb.Append("""
  </nav>
</aside>
""");
        return sb.ToString();
    }

    static void AppendModuleLinks(StringBuilder sb, ReportDataContext data, NavBadgeCounts badges)
    {
        var modules = new (string hash, string label, int badge)[]
        {
            ("rendering", "渲染模块性能", badges.Rendering),
            ("sync", "GPU同步模块性能", 0),
            ("logic", "逻辑代码模块性能", 0),
            ("ui", "UI模块性能", 0),
            ("loading", "加载模块性能", badges.Loading),
            ("physics", "物理系统性能", badges.Physics),
            ("animation", "动画模块性能", 0),
            ("particles", "粒子系统性能", 0)
        };

        foreach (var (hash, label, badge) in modules)
        {
            var exists = data.ModuleTime.Modules.Any(item => item.Key == hash);
            if (!exists)
            {
                continue;
            }

            AppendLink(sb, "#module-" + hash, label, badge, sub: true);
        }
    }

    static void AppendGroupStart(StringBuilder sb, string groupId, string title, int badge = 0)
    {
        sb.Append("<div class=\"sidebar-group\" data-group=\"").Append(groupId).Append("\">");
        sb.Append("<button type=\"button\" class=\"sidebar-group-title\" aria-expanded=\"true\">");
        sb.Append(WebUtility.HtmlEncode(title));
        if (badge > 0)
        {
            sb.Append("<span class=\"sidebar-badge\">").Append(badge).Append("</span>");
        }

        sb.Append("<span class=\"sidebar-chevron\">▾</span></button><div class=\"sidebar-group-items\">");
    }

    static void AppendGroupEnd(StringBuilder sb) => sb.Append("</div></div>");

    static void AppendLink(
        StringBuilder sb,
        string href,
        string label,
        int badge,
        bool sub = false,
        string? moduleNav = null)
    {
        var css = sub ? "sidebar-sub" : "sidebar-item";
        if (moduleNav != null)
        {
            css += " module-nav-link";
        }

        sb.Append("<a class=\"").Append(css).Append("\" href=\"").Append(href).Append("\"");
        if (moduleNav != null)
        {
            sb.Append(" data-module-nav=\"").Append(WebUtility.HtmlEncode(moduleNav)).Append("\"");
        }

        sb.Append(">").Append(WebUtility.HtmlEncode(label));
        if (badge > 0)
        {
            sb.Append("<span class=\"sidebar-badge\">").Append(badge).Append("</span>");
        }

        sb.Append("</a>");
    }
}
