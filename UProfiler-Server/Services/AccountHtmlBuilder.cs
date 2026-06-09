using System.Net;
using System.Text;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class AccountHtmlBuilder
{
    public enum AccountSection
    {
        Profile,
        Account,
        Invoice,
        Balance,
        Orders
    }

    public static string BuildLoginPage(AuthSettings settings, string? errorMessage = null, string? returnUrl = null)
    {
        var feishuReady = AuthSettingsLoader.IsFeishuConfigured(settings);
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\" />");
        sb.Append("<title>登录 - UProfiler</title>");
        sb.Append(PortalHtmlBuilder.PortalHeadLinks());
        sb.Append("<link rel=\"stylesheet\" href=\"").Append(StaticAssets.Css("account.css")).Append("\" />");
        sb.Append("</head><body>");
        sb.Append(PortalHtmlBuilder.BuildTopNav(PortalHtmlBuilder.NavTab.Home, null));
        sb.Append("""
<div class="login-page">
  <div class="login-card">
    <h1>登录 UProfiler</h1>
    <p class="login-desc">使用飞书账号登录，管理性能报告与账户信息。</p>
""");

        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            sb.Append("<div class=\"login-error\">").Append(WebUtility.HtmlEncode(errorMessage)).Append("</div>");
        }

        if (feishuReady)
        {
            var feishuHref = "/auth/feishu";
            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/'))
            {
                feishuHref += "?returnUrl=" + Uri.EscapeDataString(returnUrl);
            }

            sb.Append("<a class=\"feishu-login-btn\" href=\"").Append(feishuHref).Append("\">");
            sb.Append("""
  <span class="feishu-icon">飞</span>
  <span>飞书登录</span>
</a>
""");
        }
        else
        {
            sb.Append("""
<div class="login-hint">
  <p>飞书登录尚未配置。请在服务器部署目录创建 <code>auth.json</code> 并填写飞书应用凭证。</p>
  <p>参考 <code>auth.example.json</code>，或设置环境变量 <code>UPROFILER_FEISHU_APP_ID</code> / <code>UPROFILER_FEISHU_APP_SECRET</code>。</p>
</div>
""");
        }

        sb.Append("</div></div>");
        sb.Append("<script defer src=\"").Append(StaticAssets.Js("account.js")).Append("\"></script>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    public static string BuildAccountPage(UserProfile user, AccountSection section)
    {
        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html lang=\"zh-CN\"><head><meta charset=\"utf-8\" /><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\" />");
        sb.Append("<title>账户设置 - UProfiler</title>");
        sb.Append(PortalHtmlBuilder.PortalHeadLinks());
        sb.Append("<link rel=\"stylesheet\" href=\"").Append(StaticAssets.Css("account.css")).Append("\" />");
        sb.Append("</head><body>");
        sb.Append(PortalHtmlBuilder.BuildTopNav(PortalHtmlBuilder.NavTab.Home, null));
        sb.Append("<div class=\"account-layout\">");
        sb.Append(BuildSidebar(section));
        sb.Append("<main class=\"account-main\">");
        sb.Append(BuildBreadcrumb(section));
        sb.Append(BuildSectionContent(user, section));
        sb.Append("</main></div>");
        sb.Append("<script defer src=\"").Append(StaticAssets.Js("account.js")).Append("\"></script>");
        sb.Append("</body></html>");
        return sb.ToString();
    }

    static string BuildSidebar(AccountSection section)
    {
        return $"""
<aside class="account-sidebar">
  <div class="account-sidebar-title">账户设置</div>
  <nav class="account-nav">
    <a class="account-nav-item {(section == AccountSection.Profile ? "active" : "")}" href="/account/profile">
      <span class="nav-icon">👤</span> 个人信息
    </a>
    <a class="account-nav-item {(section == AccountSection.Account ? "active" : "")}" href="/account/settings">
      <span class="nav-icon">📋</span> 账户资料
    </a>
    <a class="account-nav-item disabled" href="#">
      <span class="nav-icon">🧾</span> 发票管理
    </a>
    <a class="account-nav-item disabled" href="#">
      <span class="nav-icon">💰</span> 账户余额
    </a>
    <a class="account-nav-item disabled" href="#">
      <span class="nav-icon">📦</span> 订单管理
    </a>
  </nav>
</aside>
""";
    }

    static string BuildBreadcrumb(AccountSection section)
    {
        var label = section switch
        {
            AccountSection.Profile => "个人信息",
            AccountSection.Account => "账户资料",
            _ => "账户设置"
        };

        return $"""
<div class="account-breadcrumb">
  <a href="/">主页</a>
  <span>/</span>
  <span>{label}</span>
</div>
""";
    }

    static string BuildSectionContent(UserProfile user, AccountSection section)
    {
        return section switch
        {
            AccountSection.Account => BuildAccountInfoPanel(user),
            _ => BuildProfileForm(user)
        };
    }

    static string BuildProfileForm(UserProfile user)
    {
        var avatar = string.IsNullOrWhiteSpace(user.AvatarUrl)
            ? ""
            : $"""<img src="{WebUtility.HtmlEncode(user.AvatarUrl)}" alt="avatar" />""";

        var avatarContent = string.IsNullOrWhiteSpace(user.AvatarUrl)
            ? WebUtility.HtmlEncode(GetInitials(user.DisplayName, user.Username))
            : avatar;

        return $"""
<form class="profile-form" id="profileForm" method="post" action="/api/account/profile">
  <div class="avatar-section">
    <div class="avatar-wrap">
      <div class="avatar-circle">{avatarContent}</div>
      <span class="avatar-badge">📷</span>
    </div>
  </div>

  <div class="form-grid">
    <label class="form-field required">
      <span class="field-label">用户名</span>
      <input type="text" name="username" value="{WebUtility.HtmlEncode(user.Username)}" placeholder="请输入用户名" required />
    </label>

    <label class="form-field">
      <span class="field-label">公司</span>
      <input type="text" name="company" value="{WebUtility.HtmlEncode(user.Company)}" placeholder="请输入公司名" />
    </label>

    <label class="form-field">
      <span class="field-label">公司电话</span>
      <input type="text" name="companyPhone" value="{WebUtility.HtmlEncode(user.CompanyPhone)}" placeholder="请输入联系电话" />
    </label>

    <label class="form-field">
      <span class="field-label">职业</span>
      <input type="text" name="occupation" value="{WebUtility.HtmlEncode(user.Occupation)}" placeholder="请输入职位" />
    </label>

    <label class="form-field">
      <span class="field-label">教育经历</span>
      <input type="text" name="education" value="{WebUtility.HtmlEncode(user.Education)}" placeholder="请输入教育经历" />
    </label>

    <label class="form-field">
      <span class="field-label">专业</span>
      <input type="text" name="major" value="{WebUtility.HtmlEncode(user.Major)}" placeholder="请输入专业" />
    </label>

    <div class="form-field birthday-field">
      <span class="field-label">生日</span>
      <div class="birthday-row">
        {BuildYearSelect(user.BirthYear)}
        {BuildMonthSelect(user.BirthMonth)}
        {BuildDaySelect(user.BirthDay)}
      </div>
    </div>

    <label class="form-field full-width">
      <span class="field-label">个人介绍</span>
      <textarea name="bio" rows="5" placeholder="请输入个人介绍">{WebUtility.HtmlEncode(user.Bio)}</textarea>
    </label>
  </div>

  <div class="form-actions">
    <button type="submit" class="btn-primary">保存</button>
    <span class="save-status" id="saveStatus"></span>
  </div>
</form>
""";
    }

    static string BuildAccountInfoPanel(UserProfile user)
    {
        return $"""
<div class="info-panel">
  <div class="info-row"><span class="info-label">显示名称</span><span>{WebUtility.HtmlEncode(user.DisplayName)}</span></div>
  <div class="info-row"><span class="info-label">飞书 Open ID</span><span class="mono">{WebUtility.HtmlEncode(user.FeishuOpenId)}</span></div>
  <div class="info-row"><span class="info-label">角色</span><span class="role-badge role-{user.Role}">{WebUtility.HtmlEncode(user.Role)}</span></div>
  <div class="info-row"><span class="info-label">注册时间</span><span>{user.CreatedAt.ToLocalTime():yyyy-MM-dd HH:mm}</span></div>
  <div class="info-row"><span class="info-label">最近登录</span><span>{(user.LastLoginAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "-")}</span></div>
</div>
""";
    }

    static string BuildYearSelect(int? selected)
    {
        var sb = new StringBuilder("<select name=\"birthYear\"><option value=\"\">年</option>");
        var currentYear = DateTime.Now.Year;
        for (var year = currentYear; year >= currentYear - 80; year--)
        {
            var selectedAttr = selected == year ? " selected" : "";
            sb.Append("<option value=\"").Append(year).Append('"').Append(selectedAttr).Append('>').Append(year).Append("年</option>");
        }

        sb.Append("</select>");
        return sb.ToString();
    }

    static string BuildMonthSelect(int? selected)
    {
        var sb = new StringBuilder("<select name=\"birthMonth\"><option value=\"\">月</option>");
        for (var month = 1; month <= 12; month++)
        {
            var selectedAttr = selected == month ? " selected" : "";
            sb.Append("<option value=\"").Append(month).Append('"').Append(selectedAttr).Append('>').Append(month).Append("月</option>");
        }

        sb.Append("</select>");
        return sb.ToString();
    }

    static string BuildDaySelect(int? selected)
    {
        var sb = new StringBuilder("<select name=\"birthDay\"><option value=\"\">日</option>");
        for (var day = 1; day <= 31; day++)
        {
            var selectedAttr = selected == day ? " selected" : "";
            sb.Append("<option value=\"").Append(day).Append('"').Append(selectedAttr).Append('>').Append(day).Append("日</option>");
        }

        sb.Append("</select>");
        return sb.ToString();
    }

    static string GetInitials(string displayName, string username)
    {
        var source = string.IsNullOrWhiteSpace(displayName) ? username : displayName;
        if (string.IsNullOrWhiteSpace(source))
        {
            return "U";
        }

        return source.Length >= 2 ? source[..2] : source;
    }
}
