using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public sealed class AuthSessionService
{
    public const string CookieName = "uprofiler_session";
    public const string StateCookieName = "uprofiler_oauth_state";

    readonly AuthSettings settings;

    public AuthSessionService(AuthSettings settings)
    {
        this.settings = settings;
    }

    public string CreateSessionCookie(UserProfile user)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddDays(settings.SessionDays).ToUnixTimeSeconds();
        var payload = new AuthSessionPayload
        {
            UserId = user.Id,
            ExpiresAt = expiresAt
        };
        var json = JsonSerializer.Serialize(payload);
        var signature = Sign(json);
        return $"{Base64UrlEncode(Encoding.UTF8.GetBytes(json))}.{signature}";
    }

    public string? ResolveUserId(string? cookieValue)
    {
        if (string.IsNullOrWhiteSpace(cookieValue))
        {
            return null;
        }

        var parts = cookieValue.Split('.', 2);
        if (parts.Length != 2)
        {
            return null;
        }

        byte[] jsonBytes;
        try
        {
            jsonBytes = Base64UrlDecode(parts[0]);
        }
        catch
        {
            return null;
        }

        var json = Encoding.UTF8.GetString(jsonBytes);
        if (!string.Equals(Sign(json), parts[1], StringComparison.Ordinal))
        {
            return null;
        }

        AuthSessionPayload? payload;
        try
        {
            payload = JsonSerializer.Deserialize<AuthSessionPayload>(json);
        }
        catch
        {
            return null;
        }

        if (payload == null || string.IsNullOrWhiteSpace(payload.UserId))
        {
            return null;
        }

        if (payload.ExpiresAt < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return null;
        }

        return payload.UserId;
    }

    public CookieOptions BuildSessionCookieOptions()
        => new()
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = false,
            Path = "/",
            MaxAge = TimeSpan.FromDays(settings.SessionDays)
        };

    public CookieOptions BuildClearCookieOptions()
        => new()
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = false,
            Path = "/",
            Expires = DateTimeOffset.UnixEpoch.DateTime
        };

    public string CreateOAuthState()
        => Guid.NewGuid().ToString("N");

    string Sign(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(settings.SessionSecret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Base64UrlEncode(hash);
    }

    static string Base64UrlEncode(byte[] bytes)
        => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }
}
