namespace UProfiler.Server.Models;

public sealed class AuthSettings
{
    public bool Enabled { get; set; }
    public bool RequireAuthForView { get; set; } = true;
    public bool RequireAuthForUpload { get; set; }
    public int SessionDays { get; set; } = 7;
    public string SessionSecret { get; set; } = "";
    public FeishuAuthSettings Feishu { get; set; } = new();
    public List<string> AdminOpenIds { get; set; } = new();
}

public sealed class FeishuAuthSettings
{
    public string AppId { get; set; } = "";
    public string AppSecret { get; set; } = "";
    public string RedirectUri { get; set; } = "";
}

public sealed class UserProfile
{
    public string Id { get; set; } = "";
    public string FeishuOpenId { get; set; } = "";
    public string FeishuUnionId { get; set; } = "";
    public string Username { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
    public string Company { get; set; } = "";
    public string CompanyPhone { get; set; } = "";
    public string Occupation { get; set; } = "";
    public string Education { get; set; } = "";
    public string Major { get; set; } = "";
    public int? BirthYear { get; set; }
    public int? BirthMonth { get; set; }
    public int? BirthDay { get; set; }
    public string Bio { get; set; } = "";
    public string Role { get; set; } = "user";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
}

public sealed class UserStoreData
{
    public List<UserProfile> Users { get; set; } = new();
}

public sealed class AuthSessionPayload
{
    public string UserId { get; set; } = "";
    public long ExpiresAt { get; set; }
}
