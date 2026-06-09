using System.Text.Json;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public sealed class UserStore
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    readonly string storePath;
    readonly SemaphoreSlim writeLock = new(1, 1);
    UserStoreData data = new();

    public UserStore(string baseDir)
    {
        storePath = Path.Combine(baseDir, "users.json");
        Load();
    }

    public UserProfile? FindById(string userId)
        => data.Users.FirstOrDefault(user => string.Equals(user.Id, userId, StringComparison.Ordinal));

    public UserProfile? FindByFeishuOpenId(string openId)
        => data.Users.FirstOrDefault(user => string.Equals(user.FeishuOpenId, openId, StringComparison.Ordinal));

    public UserProfile UpsertFromFeishu(
        string openId,
        string unionId,
        string name,
        string avatarUrl,
        IEnumerable<string> adminOpenIds)
    {
        var now = DateTime.UtcNow;
        var existing = FindByFeishuOpenId(openId);
        if (existing != null)
        {
            existing.DisplayName = string.IsNullOrWhiteSpace(name) ? existing.DisplayName : name;
            existing.AvatarUrl = string.IsNullOrWhiteSpace(avatarUrl) ? existing.AvatarUrl : avatarUrl;
            existing.FeishuUnionId = string.IsNullOrWhiteSpace(unionId) ? existing.FeishuUnionId : unionId;
            existing.LastLoginAt = now;
            existing.UpdatedAt = now;
            if (string.IsNullOrWhiteSpace(existing.Username))
            {
                existing.Username = BuildDefaultUsername(name, openId);
            }

            ApplyAdminRole(existing, adminOpenIds);
            Save();
            return existing;
        }

        var user = new UserProfile
        {
            Id = Guid.NewGuid().ToString("N"),
            FeishuOpenId = openId,
            FeishuUnionId = unionId,
            Username = BuildDefaultUsername(name, openId),
            DisplayName = name,
            AvatarUrl = avatarUrl,
            CreatedAt = now,
            UpdatedAt = now,
            LastLoginAt = now
        };
        ApplyAdminRole(user, adminOpenIds);
        data.Users.Add(user);
        Save();
        return user;
    }

    public bool UpdateProfile(string userId, UserProfileUpdate update)
    {
        var user = FindById(userId);
        if (user == null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(update.Username))
        {
            user.Username = update.Username.Trim();
        }

        user.Company = update.Company?.Trim() ?? "";
        user.CompanyPhone = update.CompanyPhone?.Trim() ?? "";
        user.Occupation = update.Occupation?.Trim() ?? "";
        user.Education = update.Education?.Trim() ?? "";
        user.Major = update.Major?.Trim() ?? "";
        user.Bio = update.Bio?.Trim() ?? "";
        user.BirthYear = update.BirthYear;
        user.BirthMonth = update.BirthMonth;
        user.BirthDay = update.BirthDay;
        user.UpdatedAt = DateTime.UtcNow;
        Save();
        return true;
    }

    static void ApplyAdminRole(UserProfile user, IEnumerable<string> adminOpenIds)
    {
        user.Role = adminOpenIds.Any(id => string.Equals(id, user.FeishuOpenId, StringComparison.OrdinalIgnoreCase))
            ? "admin"
            : "user";
    }

    static string BuildDefaultUsername(string name, string openId)
    {
        var baseName = string.IsNullOrWhiteSpace(name) ? "user" : name.Trim();
        baseName = new string(baseName.Where(ch => char.IsLetterOrDigit(ch) || ch is '_' or '-').ToArray());
        if (string.IsNullOrWhiteSpace(baseName))
        {
            baseName = "user";
        }

        return baseName.Length > 24 ? baseName[..24] : baseName + "_" + openId[^4..];
    }

    void Load()
    {
        if (!File.Exists(storePath))
        {
            return;
        }

        try
        {
            var json = File.ReadAllText(storePath);
            data = JsonSerializer.Deserialize<UserStoreData>(json, JsonOptions) ?? new UserStoreData();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: failed to read users.json: {ex.Message}");
            data = new UserStoreData();
        }
    }

    async Task SaveAsync()
    {
        await writeLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(storePath, json);
        }
        finally
        {
            writeLock.Release();
        }
    }

    void Save()
    {
        writeLock.Wait();
        try
        {
            var json = JsonSerializer.Serialize(data, JsonOptions);
            File.WriteAllText(storePath, json);
        }
        finally
        {
            writeLock.Release();
        }
    }
}

public sealed class UserProfileUpdate
{
    public string? Username { get; set; }
    public string? Company { get; set; }
    public string? CompanyPhone { get; set; }
    public string? Occupation { get; set; }
    public string? Education { get; set; }
    public string? Major { get; set; }
    public int? BirthYear { get; set; }
    public int? BirthMonth { get; set; }
    public int? BirthDay { get; set; }
    public string? Bio { get; set; }
}
