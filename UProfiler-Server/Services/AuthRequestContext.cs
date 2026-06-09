using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class AuthRequestContext
{
    static readonly AsyncLocal<UserProfile?> CurrentUser = new();
    static readonly AsyncLocal<AuthSettings?> CurrentSettings = new();

    public static void Set(UserProfile? user, AuthSettings settings)
    {
        CurrentUser.Value = user;
        CurrentSettings.Value = settings;
    }

    public static void Clear()
    {
        CurrentUser.Value = null;
        CurrentSettings.Value = null;
    }

    public static UserProfile? Current => CurrentUser.Value;
    public static AuthSettings? Settings => CurrentSettings.Value;
}
