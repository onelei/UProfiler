using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class PortalHtmlBuilder
{
    public enum NavTab
    {
        Home,
        Project,
        Performance,
        Report
    }

    public static string BuildProjectsHome(PortalCatalog catalog)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\" />");
        sb.Append("<title>UProfiler - 项目</title>");
        sb.Append(PortalHeadLinks());
        sb.Append("</head><body>");
        sb.Append(BuildTopNav(NavTab.Home, null));
        sb.Append("<div class=\"page\">");
        sb.Append(BuildProjectTabs(catalog, null));
        sb.Append("<div class=\"home-layout\">");
        sb.Append("<div class=\"home-main\">");
        sb.Append("<div class=\"project-grid\">");

        if (catalog.Projects.Count == 0)
        {
            sb.Append("""
<div class="empty-box">
  <h3>暂无项目</h3>
  <p>请先在 Unity 中运行 UProfiler 并完成一次监控上传。</p>
</div>
""");
        }
        else
        {
            var index = 1;
            foreach (var project in catalog.Projects)
            {
                sb.Append(BuildProjectCard(project, index++));
            }
        }

        sb.Append("</div></div>");
        sb.Append(BuildActivitySidebar(catalog.RecentSessions, null));
        sb.Append("<script defer src=\"").Append(StaticAssets.Js("account.js")).Append("\"></script>");
        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    public static string BuildProjectDetail(ProjectSummary project, PortalCatalog catalog)
    {
        var latest = project.LatestSession;
        var pkgUrl = ProjectCatalog.EncodePackage(project.PackageName);
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\" />");
        sb.Append("<title>").Append(WebUtility.HtmlEncode(project.ProductName)).Append(" - 项目</title>");
        sb.Append(PortalHeadLinks());
        sb.Append("</head><body>");
        sb.Append(BuildTopNav(NavTab.Project, project.PackageName));
        sb.Append("<div class=\"page\">");
        sb.Append(BuildProjectTabs(catalog, project.PackageName));
        sb.Append("<div class=\"detail-layout\">");

        sb.Append("<div class=\"detail-left\">");
        sb.Append("<div class=\"project-icon\">").Append(WebUtility.HtmlEncode(GetInitials(project.ProductName))).Append("</div>");
        sb.Append("<h2>").Append(WebUtility.HtmlEncode(project.ProductName)).Append("</h2>");
        sb.Append("<div class=\"meta-line\">").Append(WebUtility.HtmlEncode(project.PackageName)).Append("</div>");
        sb.Append("<div class=\"meta-line\">报告 ").Append(project.ReportCount).Append(" 份</div>");
        sb.Append("<div class=\"meta-line\">平台 ").Append(WebUtility.HtmlEncode(project.Platform)).Append("</div>");
        if (latest != null)
        {
            sb.Append("<div class=\"grade-badge grade-").Append(latest.Grade.ToLowerInvariant()).Append("\">").Append(latest.Grade).Append("</div>");
        }
        sb.Append("</div>");

        sb.Append("<div class=\"detail-center\">");
        sb.Append("<div class=\"panel-head\"><h3>测试服务</h3></div>");
        sb.Append("<div class=\"service-list\">");
        sb.Append($"""
<a class="service-item" href="/project/{pkgUrl}/performance">
  <div class="service-icon">📊</div>
  <div class="service-body">
    <div class="service-title">总体性能分析</div>
    <div class="service-desc">GOT Online · 帧率 / 内存 / 渲染 / 诊断</div>
  </div>
  <div class="service-count">{project.ReportCount}</div>
  <div class="service-arrow">›</div>
</a>
""");

        sb.Append("""
<div class="service-item disabled">
  <div class="service-icon">🔧</div>
  <div class="service-body"><div class="service-title">运行时资源检测</div><div class="service-desc">本地版暂未接入</div></div>
  <div class="service-count">-</div>
</div>
<div class="service-item disabled">
  <div class="service-icon">⚡</div>
  <div class="service-body"><div class="service-title">Mono 性能分析</div><div class="service-desc">见总体性能报告内函数表</div></div>
  <div class="service-count">-</div>
</div>
""");
        sb.Append("</div>");

        if (latest != null)
        {
            sb.Append("<div class=\"panel-head\" style=\"margin-top:24px\"><h3>最新报告概览</h3>");
            sb.Append("<a class=\"link-btn\" href=\"/report_").Append(WebUtility.HtmlEncode(latest.SessionKey)).Append(".html\">查看完整报告</a></div>");
            sb.Append(BuildLatestPreview(latest));
        }

        sb.Append("</div>");
        sb.Append(BuildActivitySidebar(project.Sessions, null));
        sb.Append("<script defer src=\"").Append(StaticAssets.Js("account.js")).Append("\"></script>");
        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    public static string BuildPerformancePage(ProjectSummary project, PortalCatalog catalog)
    {
        var pkgUrl = ProjectCatalog.EncodePackage(project.PackageName);
        var trendJson = JsonSerializer.Serialize(BuildTrendData(project.Sessions));
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\" />");
        sb.Append("<title>").Append(WebUtility.HtmlEncode(project.ProductName)).Append(" - 总体性能分析</title>");
        sb.Append(PortalHeadLinks());
        sb.Append("</head><body>");
        sb.Append(BuildTopNav(NavTab.Performance, project.PackageName));
        sb.Append("<div class=\"page\">");
        sb.Append(BuildProjectTabs(catalog, project.PackageName));

        var latest = project.LatestSession;
        sb.Append("<div class=\"summary-row\">");
        sb.Append("<div class=\"summary-card project-mini\">");
        sb.Append("<div class=\"mini-icon\">").Append(WebUtility.HtmlEncode(GetInitials(project.ProductName))).Append("</div>");
        sb.Append("<div><div class=\"mini-title\">").Append(WebUtility.HtmlEncode(project.ProductName)).Append("</div>");
        sb.Append("<div class=\"mini-sub\">").Append(WebUtility.HtmlEncode(project.PackageName)).Append("</div></div></div>");

        if (latest != null)
        {
            sb.Append($"""
<div class="summary-card perf-card">
  <div class="perf-grade">{latest.Grade}</div>
  <div class="perf-score">{latest.Score:F1}</div>
  <div class="perf-label">运行性能</div>
  <div class="perf-stats">
    <span>FPS 问题 {Math.Max(0, latest.DiagnosisHigh)}</span>
    <span>平均 FPS {latest.AvgFps:F1}</span>
  </div>
</div>
""");
        }

        sb.Append("<div class=\"summary-card stat-card\"><div class=\"stat-num\">").Append(project.ReportCount).Append("</div><div class=\"stat-label\">检测报告</div></div>");
        sb.Append("</div>");

        sb.Append("<div class=\"perf-layout\">");
        sb.Append($"""
<aside class="perf-sidebar">
  <div class="perf-sidebar-title">检测记录</div>
  <div class="platform-tabs"><span class="active">全部</span></div>
  <div class="sidebar-group">
    <div class="sidebar-group-title">GOT Online <span>{project.ReportCount}</span></div>
    <a class="sidebar-link active" href="/project/{pkgUrl}/performance">总体性能分析 <span>{project.ReportCount}</span></a>
    <div class="sidebar-link disabled">Mono 性能分析</div>
    <div class="sidebar-link disabled">运行时资源检测</div>
  </div>
</aside>
""");

        sb.Append("<div class=\"perf-main\">");
        sb.Append("<div class=\"chart-panel\"><div class=\"panel-head\"><h3>项目数据趋势</h3></div><div id=\"trendChart\" class=\"trend-chart\"><div class=\"chart-loading\">滚动到此处加载图表…</div></div></div>");
        sb.Append("<div class=\"table-panel\"><div class=\"panel-head\"><h3>报告列表</h3></div>");
        sb.Append("<table class=\"report-table\"><thead><tr>");
        sb.Append("<th>状态</th><th>版本</th><th>报告名称</th><th>测试机型</th><th>测试日期</th><th>平均 FPS</th><th>评级</th><th>操作</th>");
        sb.Append("</tr></thead><tbody>");

        foreach (var session in project.Sessions)
        {
            var dateText = FormatSessionDate(session.SessionKey);
            var reportName = $"{session.DeviceModel}_{dateText}_overview";
            sb.Append("<tr>");
            sb.Append("<td><span class=\"status-ok\">已完成</span></td>");
            sb.Append("<td>").Append(WebUtility.HtmlEncode(session.Version)).Append("</td>");
            sb.Append("<td>").Append(WebUtility.HtmlEncode(reportName)).Append("</td>");
            sb.Append("<td>").Append(WebUtility.HtmlEncode(session.DeviceModel)).Append("</td>");
            sb.Append("<td>").Append(WebUtility.HtmlEncode(dateText)).Append("</td>");
            sb.Append("<td>").Append(session.AvgFps.ToString("F1", CultureInfo.InvariantCulture)).Append("</td>");
            sb.Append("<td><span class=\"grade-pill grade-").Append(session.Grade.ToLowerInvariant()).Append("\">").Append(session.Grade).Append("</span></td>");
            sb.Append("<td><a class=\"link-btn\" href=\"/report_").Append(WebUtility.HtmlEncode(session.SessionKey)).Append(".html\">查看报告</a></td>");
            sb.Append("</tr>");
        }

        if (project.Sessions.Count == 0)
        {
            sb.Append("<tr><td colspan=\"8\" class=\"empty-cell\">暂无报告</td></tr>");
        }

        sb.Append("</tbody></table></div></div></div>");
        sb.Append("<script>window.trendData = ").Append(trendJson).Append(";</script>");
        sb.Append("<script defer src=\"").Append(StaticAssets.Js("portal-trend.js")).Append("\"></script>");
        sb.Append("<script defer src=\"").Append(StaticAssets.Js("account.js")).Append("\"></script>");
        sb.Append("</div></body></html>");
        return sb.ToString();
    }

    static object BuildTrendData(List<SessionSummary> sessions)
    {
        var ordered = sessions.OrderBy(item => item.SessionKey, StringComparer.Ordinal).ToList();
        return new
        {
            labels = ordered.Select(item => FormatSessionDate(item.SessionKey)).ToArray(),
            fps = ordered.Select(item => Math.Round(item.AvgFps, 1)).ToArray(),
            dc = ordered.Select(item => Math.Round(item.PeakDrawCall / 10.0, 2)).ToArray()
        };
    }

    static string BuildLatestPreview(SessionSummary latest)
    {
        return $"""
<div class="latest-preview">
  <div class="preview-card"><div class="label">平均 FPS</div><div class="value">{latest.AvgFps:F1}</div></div>
  <div class="preview-card"><div class="label">最低 FPS</div><div class="value">{latest.MinFps}</div></div>
  <div class="preview-card"><div class="label">DrawCall 峰值</div><div class="value">{latest.PeakDrawCall}</div></div>
  <div class="preview-card"><div class="label">HIGH 诊断</div><div class="value">{latest.DiagnosisHigh}</div></div>
</div>
<p class="preview-meta">最近测试：{WebUtility.HtmlEncode(latest.TestTime)} · {WebUtility.HtmlEncode(FormatSessionDate(latest.SessionKey))}</p>
""";
    }

    static string BuildProjectCard(ProjectSummary project, int index)
    {
        var latest = project.LatestSession;
        var pkgUrl = ProjectCatalog.EncodePackage(project.PackageName);
        var grade = latest?.Grade ?? "B";
        var dateText = latest != null ? FormatSessionDate(latest.SessionKey) : "-";

        return $"""
<a class="project-card" href="/project/{pkgUrl}">
  <div class="card-top">
    <span class="card-no">No. {index:D4}</span>
    <span class="card-grade grade-{grade.ToLowerInvariant()}">{grade}</span>
  </div>
  <div class="card-body">
    <div class="card-icon">{WebUtility.HtmlEncode(GetInitials(project.ProductName))}</div>
    <div>
      <div class="card-title">{WebUtility.HtmlEncode(project.ProductName)}</div>
      <div class="card-sub">{WebUtility.HtmlEncode(project.PackageName)}</div>
      <div class="card-date">{WebUtility.HtmlEncode(dateText)}</div>
    </div>
  </div>
</a>
""";
    }

    static string BuildActivitySidebar(IEnumerable<SessionSummary> sessions, string? currentPackage)
    {
        var list = sessions.Take(12).ToList();
        var sb = new StringBuilder();
        sb.Append("<aside class=\"activity-sidebar\">");
        sb.Append("<div class=\"activity-title\">项目动态</div>");
        sb.Append("<div class=\"activity-badge\">新的报告 (").Append(list.Count).Append(")</div>");
        sb.Append("<div class=\"activity-list\">");

        foreach (var session in list)
        {
            if (currentPackage != null
                && !string.Equals(session.PackageName, currentPackage, StringComparison.OrdinalIgnoreCase)
                && !string.Equals(session.ProductName, currentPackage, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dateText = session.UploadedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? FormatSessionDate(session.SessionKey);
            sb.Append("<a class=\"activity-item\" href=\"/report_").Append(WebUtility.HtmlEncode(session.SessionKey)).Append(".html\">");
            sb.Append("<div class=\"activity-time\">").Append(WebUtility.HtmlEncode(dateText)).Append("</div>");
            sb.Append("<div class=\"activity-type\">GOT Online - Overview</div>");
            sb.Append("<div class=\"activity-row\">");
            sb.Append("<span class=\"activity-icon\">").Append(WebUtility.HtmlEncode(GetInitials(session.ProductName))).Append("</span>");
            sb.Append("<span>").Append(WebUtility.HtmlEncode(session.ProductName)).Append("</span></div>");
            sb.Append("<div class=\"activity-link\">").Append(WebUtility.HtmlEncode(session.DeviceModel)).Append("_").Append(WebUtility.HtmlEncode(FormatSessionDate(session.SessionKey))).Append("_overview</div>");
            sb.Append("</a>");
        }

        if (!list.Any())
        {
            sb.Append("<div class=\"activity-empty\">暂无动态</div>");
        }

        sb.Append("</div></aside>");
        return sb.ToString();
    }

    static string BuildProjectTabs(PortalCatalog catalog, string? activePackage)
    {
        var sb = new StringBuilder();
        sb.Append("<div class=\"project-tabs\">");
        sb.Append("<a class=\"tab-item ").Append(activePackage == null ? "active" : "").Append("\" href=\"/\"><span class=\"tab-grid\">▦</span> 全部</a>");

        foreach (var project in catalog.Projects.Take(6))
        {
            var pkgUrl = ProjectCatalog.EncodePackage(project.PackageName);
            var active = string.Equals(project.PackageName, activePackage, StringComparison.OrdinalIgnoreCase) ? " active" : "";
            sb.Append("<a class=\"tab-item").Append(active).Append("\" href=\"/project/").Append(pkgUrl).Append("\">");
            sb.Append("<span class=\"tab-icon\">").Append(WebUtility.HtmlEncode(GetInitials(project.ProductName))).Append("</span> ");
            sb.Append(WebUtility.HtmlEncode(project.ProductName));
            sb.Append("</a>");
        }

        sb.Append("</div>");
        return sb.ToString();
    }

    public static string BuildTopNav(NavTab tab, string? packageName)
    {
        var pkgUrl = packageName == null ? null : ProjectCatalog.EncodePackage(packageName);
        var user = AuthRequestContext.Current;
        var auth = AuthRequestContext.Settings;
        var sb = new StringBuilder();
        sb.Append("<header class=\"top-nav\">");
        sb.Append("<div class=\"nav-left\"><a class=\"logo\" href=\"/\">UProfiler</a><span class=\"logo-sub\">MAKE IT SIMPLE</span></div>");
        sb.Append("<nav class=\"nav-menu\">");
        sb.Append("<a class=\"nav-link home-link\" href=\"/\">首页</a>");
        if (pkgUrl != null)
        {
            sb.Append("<a class=\"nav-link").Append(tab == NavTab.Project ? " active" : "").Append("\" href=\"/project/").Append(pkgUrl).Append("\">项目</a>");
            sb.Append("<a class=\"nav-link").Append(tab == NavTab.Performance ? " active" : "").Append("\" href=\"/project/").Append(pkgUrl).Append("/performance\">总体性能分析</a>");
        }
        sb.Append("</nav>");
        sb.Append(BuildUserMenu(user, auth));
        sb.Append("</header>");
        return sb.ToString();
    }

    static string BuildUserMenu(Models.UserProfile? user, Models.AuthSettings? auth)
    {
        if (auth == null || !auth.Enabled)
        {
            return "";
        }

        if (user == null)
        {
            return """<div class="nav-right"><a class="login-link" href="/login">登录</a></div>""";
        }

        var displayName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.Username : user.DisplayName;
        var avatarContent = string.IsNullOrWhiteSpace(user.AvatarUrl)
            ? WebUtility.HtmlEncode(GetInitials(displayName))
            : $"""<img src="{WebUtility.HtmlEncode(user.AvatarUrl)}" alt="avatar" />""";

        return $"""
<div class="nav-right">
  <div class="user-menu">
    <button type="button" class="user-trigger" aria-label="账户菜单">
      <span class="user-avatar">{avatarContent}</span>
      <span class="user-name">{WebUtility.HtmlEncode(displayName)}</span>
    </button>
    <div class="user-dropdown">
      <a class="dropdown-item" href="/account/profile">账户设置</a>
      <a class="dropdown-item" href="/account/settings">账户资料</a>
      <div class="dropdown-divider"></div>
      <form method="post" action="/auth/logout" style="margin:0">
        <button type="submit" class="dropdown-item danger" style="width:100%;border:none;background:transparent;text-align:left;cursor:pointer">退出登录</button>
      </form>
    </div>
  </div>
</div>
""";
    }

    static string FormatSessionDate(string sessionKey)
    {
        if (DateTime.TryParseExact(sessionKey, "yyyy_MM_dd_HH_mm_ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        return sessionKey.Replace('_', '-');
    }

    static string GetInitials(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "MT";
        }

        return name.Length >= 2 ? name[..2] : name;
    }

    public static string PortalHeadLinks()
        => $"""<link rel="stylesheet" href="{StaticAssets.Css("portal.css")}" /><link rel="stylesheet" href="{StaticAssets.Css("account.css")}" />""";

    public static string GetTopNavStyles()
        => $"""<link rel="stylesheet" href="{StaticAssets.Css("portal.css")}" /><link rel="stylesheet" href="{StaticAssets.Css("account.css")}" /><script defer src="{StaticAssets.Js("account.js")}"></script>""";
}
