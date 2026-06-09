using System.Text.Json;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class AuthSettingsLoader
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public static AuthSettings Load(string baseDir, int port)
    {
        var settings = new AuthSettings();
        var configPath = Path.Combine(baseDir, "auth.json");
        if (File.Exists(configPath))
        {
            try
            {
                var json = File.ReadAllText(configPath);
                var loaded = JsonSerializer.Deserialize<AuthSettings>(json, JsonOptions);
                if (loaded != null)
                {
                    settings = loaded;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: failed to read auth.json: {ex.Message}");
            }
        }

        ApplyEnvironmentOverrides(settings, port);
        Normalize(settings, port);
        return settings;
    }

    static void ApplyEnvironmentOverrides(AuthSettings settings, int port)
    {
        if (TryParseBool(Environment.GetEnvironmentVariable("UPROFILER_AUTH_ENABLED"), out var enabled))
        {
            settings.Enabled = enabled;
        }

        if (TryParseBool(Environment.GetEnvironmentVariable("UPROFILER_AUTH_REQUIRE_VIEW"), out var requireView))
        {
            settings.RequireAuthForView = requireView;
        }

        if (TryParseBool(Environment.GetEnvironmentVariable("UPROFILER_AUTH_REQUIRE_UPLOAD"), out var requireUpload))
        {
            settings.RequireAuthForUpload = requireUpload;
        }

        var secret = Environment.GetEnvironmentVariable("UPROFILER_AUTH_SECRET");
        if (!string.IsNullOrWhiteSpace(secret))
        {
            settings.SessionSecret = secret;
        }

        var appId = Environment.GetEnvironmentVariable("UPROFILER_FEISHU_APP_ID");
        if (!string.IsNullOrWhiteSpace(appId))
        {
            settings.Feishu.AppId = appId;
        }

        var appSecret = Environment.GetEnvironmentVariable("UPROFILER_FEISHU_APP_SECRET");
        if (!string.IsNullOrWhiteSpace(appSecret))
        {
            settings.Feishu.AppSecret = appSecret;
        }

        var redirectUri = Environment.GetEnvironmentVariable("UPROFILER_FEISHU_REDIRECT_URI");
        if (!string.IsNullOrWhiteSpace(redirectUri))
        {
            settings.Feishu.RedirectUri = redirectUri;
        }

        var adminIds = Environment.GetEnvironmentVariable("UPROFILER_ADMIN_OPEN_IDS");
        if (!string.IsNullOrWhiteSpace(adminIds))
        {
            settings.AdminOpenIds = adminIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        if (int.TryParse(Environment.GetEnvironmentVariable("UPROFILER_AUTH_SESSION_DAYS"), out var sessionDays)
            && sessionDays > 0)
        {
            settings.SessionDays = sessionDays;
        }
    }

    static void Normalize(AuthSettings settings, int port)
    {
        if (string.IsNullOrWhiteSpace(settings.SessionSecret))
        {
            settings.SessionSecret = $"uprofiler-local-{port}-change-me";
        }

        if (string.IsNullOrWhiteSpace(settings.Feishu.RedirectUri))
        {
            settings.Feishu.RedirectUri = $"http://localhost:{port}/auth/feishu/callback";
        }

        settings.AdminOpenIds = settings.AdminOpenIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    static bool TryParseBool(string? value, out bool result)
    {
        result = false;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (bool.TryParse(value, out result))
        {
            return true;
        }

        result = value is "1" or "yes" or "on";
        return true;
    }

    public static bool IsFeishuConfigured(AuthSettings settings)
        => settings.Enabled
           && !string.IsNullOrWhiteSpace(settings.Feishu.AppId)
           && !string.IsNullOrWhiteSpace(settings.Feishu.AppSecret);
}
