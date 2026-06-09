using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public sealed class FeishuOAuthService
{
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    readonly AuthSettings settings;
    readonly HttpClient httpClient = new();

    public FeishuOAuthService(AuthSettings settings)
    {
        this.settings = settings;
    }

    public string BuildAuthorizeUrl(string state)
    {
        var redirectUri = Uri.EscapeDataString(settings.Feishu.RedirectUri);
        return $"https://accounts.feishu.cn/open-apis/authen/v1/authorize"
               + $"?client_id={Uri.EscapeDataString(settings.Feishu.AppId)}"
               + $"&redirect_uri={redirectUri}"
               + "&response_type=code"
               + $"&state={Uri.EscapeDataString(state)}";
    }

    public async Task<FeishuUserInfo?> ExchangeCodeAsync(string code)
    {
        var appToken = await GetAppAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(appToken))
        {
            return null;
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://open.feishu.cn/open-apis/authen/v1/oidc/access_token");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", appToken);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new { grant_type = "authorization_code", code }),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Feishu token exchange failed: {body}");
            return null;
        }

        var tokenResult = JsonSerializer.Deserialize<FeishuApiResponse<FeishuTokenData>>(body, JsonOptions);
        var tokenData = tokenResult?.Data;
        if (tokenData == null)
        {
            Console.WriteLine($"Feishu token response invalid: {body}");
            return null;
        }

        return new FeishuUserInfo
        {
            OpenId = tokenData.OpenId ?? "",
            UnionId = tokenData.UnionId ?? "",
            Name = tokenData.Name ?? tokenData.EnName ?? "",
            AvatarUrl = tokenData.AvatarUrl ?? tokenData.AvatarThumb ?? ""
        };
    }

    async Task<string?> GetAppAccessTokenAsync()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "https://open.feishu.cn/open-apis/auth/v3/app_access_token/internal");
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                app_id = settings.Feishu.AppId,
                app_secret = settings.Feishu.AppSecret
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await httpClient.SendAsync(request);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Feishu app token failed: {body}");
            return null;
        }

        var result = JsonSerializer.Deserialize<FeishuAppTokenResponse>(body, JsonOptions);
        return result?.AppAccessToken;
    }
}

public sealed class FeishuUserInfo
{
    public string OpenId { get; set; } = "";
    public string UnionId { get; set; } = "";
    public string Name { get; set; } = "";
    public string AvatarUrl { get; set; } = "";
}

sealed class FeishuApiResponse<T>
{
    public int Code { get; set; }
    public string? Msg { get; set; }
    public T? Data { get; set; }
}

sealed class FeishuTokenData
{
    public string? AccessToken { get; set; }
    public string? OpenId { get; set; }
    public string? UnionId { get; set; }
    public string? Name { get; set; }
    public string? EnName { get; set; }
    public string? AvatarUrl { get; set; }
    public string? AvatarThumb { get; set; }
}

sealed class FeishuAppTokenResponse
{
    public int Code { get; set; }
    public string? Msg { get; set; }
    public string? AppAccessToken { get; set; }
    public int Expire { get; set; }
}
