using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class AuthMiddlewareHelper
{
    public static bool IsPublicPath(string path)
    {
        if (path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/auth/", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/login", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }

    public static bool RequiresViewAuth(AuthSettings settings, string path, string method)
    {
        if (!settings.Enabled || !settings.RequireAuthForView)
        {
            return false;
        }

        if (IsPublicPath(path))
        {
            return false;
        }

        if (path.StartsWith("/account", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (path.StartsWith("/api/account", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return path == "/"
                   || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase)
                   || path.StartsWith("/project/", StringComparison.OrdinalIgnoreCase)
                   || (path.StartsWith("/report_", StringComparison.OrdinalIgnoreCase)
                       && path.EndsWith(".html", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    public static bool RequiresUploadAuth(AuthSettings settings, string path)
    {
        if (!settings.Enabled || !settings.RequireAuthForUpload)
        {
            return false;
        }

        return path.Contains("TestHandler.ashx", StringComparison.OrdinalIgnoreCase)
               || path.Contains("ReceiveDataHandler.ashx", StringComparison.OrdinalIgnoreCase);
    }

    public static UserProfile? ResolveCurrentUser(
        HttpContext context,
        AuthSessionService sessionService,
        UserStore userStore)
    {
        context.Request.Cookies.TryGetValue(AuthSessionService.CookieName, out var cookieValue);
        var userId = sessionService.ResolveUserId(cookieValue);
        return userId == null ? null : userStore.FindById(userId);
    }
}
