using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using UProfiler.Server.Models;
using UProfiler.Server.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.FileProviders;

var requestedPort = ResolvePort(args);
var baseDir = AppContext.BaseDirectory;
var uploadDir = Path.Combine(baseDir, "uploads");
var logDir = Path.Combine(baseDir, "logs");
var reportDir = Path.Combine(baseDir, "reports");
Directory.CreateDirectory(uploadDir);
Directory.CreateDirectory(logDir);
Directory.CreateDirectory(reportDir);

if (TryGenerateReportOnly(args, uploadDir, reportDir, out var generatedReportPath))
{
    Console.WriteLine($"Generated report: {generatedReportPath}");
    return;
}

var port = FindAvailablePort(requestedPort);

var authSettings = AuthSettingsLoader.Load(baseDir, port);
var userStore = new UserStore(baseDir);
var authSessionService = new AuthSessionService(authSettings);
var feishuOAuthService = new FeishuOAuthService(authSettings);

var serverState = new ServerState();
var uploadIndex = new UploadIndex(uploadDir);
var reportGenerator = new ReportGenerator(uploadIndex, reportDir);
var projectCatalog = new ProjectCatalog(uploadIndex, reportDir);

if (port != requestedPort)
{
    Console.WriteLine($"Port {requestedPort} is in use, switched to {port}.");
    Console.WriteLine($"If Unity upload fails, set Config.Port = {port} in UProfiler-Unity/Packages/com.lemonframework.uprofiler/Runtime/Scripts/Core/Config.cs");
    Console.WriteLine();
}

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(port);
    options.Limits.MaxRequestBodySize = null;
});
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = long.MaxValue;
    options.MemoryBufferThreshold = int.MaxValue;
});
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = false;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[] { "text/html" });
});
builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});

var app = builder.Build();
var staticWebRoot = ResolveStaticWebRoot(baseDir);
Console.WriteLine($"Static web root: {staticWebRoot}");

app.UseResponseCompression();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(staticWebRoot),
    RequestPath = "",
    OnPrepareResponse = context =>
    {
        context.Context.Response.Headers.CacheControl = "public,max-age=604800,immutable";
    }
});

app.MapMethods("/{**path}", new[] { "GET", "POST", "PUT", "OPTIONS" }, async (HttpContext context) =>
{
    var path = context.Request.Path.Value ?? "/";
    var method = context.Request.Method;

    try
    {
        var currentUser = AuthMiddlewareHelper.ResolveCurrentUser(context, authSessionService, userStore);
        AuthRequestContext.Set(currentUser, authSettings);

        IFormCollection? form = null;
        if (context.Request.HasFormContentType)
        {
            form = await context.Request.ReadFormAsync();
        }

        if (method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            return Results.Ok();
        }

        if (TryServeStaticFile(path, staticWebRoot, out var staticFileResult))
        {
            return staticFileResult;
        }

        if (TryHandleAuthRoutes(
                context,
                path,
                method,
                form,
                authSettings,
                authSessionService,
                feishuOAuthService,
                userStore,
                out var authResult))
        {
            return authResult;
        }

        if (AuthMiddlewareHelper.RequiresUploadAuth(authSettings, path) && currentUser == null)
        {
            return Results.Text("unauthorized", "text/plain", Encoding.UTF8, (int)HttpStatusCode.Unauthorized);
        }

        if (AuthMiddlewareHelper.RequiresViewAuth(authSettings, path, method) && currentUser == null)
        {
            var returnUrl = Uri.EscapeDataString(path + context.Request.QueryString);
            return Results.Redirect($"/login?returnUrl={returnUrl}");
        }

        if (path.StartsWith("/capture/", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            return TryServeCaptureImage(path, uploadIndex);
        }

        await SafeLogRequestAsync(serverState, context, path, form);

        if (path.Contains("ReceiveDataHandler.ashx", StringComparison.OrdinalIgnoreCase))
        {
            var sessionKey = context.Request.Query["TestTime"].ToString();
            var packageName = context.Request.Query["PackageName"].ToString();
            if (!string.IsNullOrWhiteSpace(sessionKey))
            {
                reportGenerator.Generate(sessionKey, packageName);
                projectCatalog.InvalidateCache();
                Console.WriteLine($"Generated report: report_{sessionKey}.html");
            }

            return Results.Text("success", "text/plain", Encoding.UTF8);
        }

        if (path.Contains("TestHandler.ashx", StringComparison.OrdinalIgnoreCase))
        {
            await SaveUploadAsync(serverState, uploadIndex, projectCatalog, context, form);
            return Results.Text("success", "text/plain", Encoding.UTF8);
        }

        if (path == "/" || path.Equals("/index.html", StringComparison.OrdinalIgnoreCase))
        {
            var catalog = projectCatalog.Build();
            return Results.Content(
                PortalHtmlBuilder.BuildProjectsHome(catalog),
                "text/html; charset=utf-8",
                Encoding.UTF8);
        }

        if (path.StartsWith("/project/", StringComparison.OrdinalIgnoreCase))
        {
            var remainder = path["/project/".Length..].Trim('/');
            var slashIndex = remainder.IndexOf('/');
            var packageEncoded = slashIndex >= 0 ? remainder[..slashIndex] : remainder;
            var subPage = slashIndex >= 0 ? remainder[(slashIndex + 1)..] : "";

            if (!string.IsNullOrWhiteSpace(packageEncoded))
            {
                var packageName = ProjectCatalog.DecodePackage(packageEncoded);
                var project = projectCatalog.FindProject(packageName);
                if (project != null)
                {
                    var catalog = projectCatalog.Build();
                    if (subPage.Equals("performance", StringComparison.OrdinalIgnoreCase))
                    {
                        return Results.Content(
                            PortalHtmlBuilder.BuildPerformancePage(project, catalog),
                            "text/html; charset=utf-8",
                            Encoding.UTF8);
                    }

                    if (string.IsNullOrWhiteSpace(subPage))
                    {
                        return Results.Content(
                            PortalHtmlBuilder.BuildProjectDetail(project, catalog),
                            "text/html; charset=utf-8",
                            Encoding.UTF8);
                    }
                }
            }
        }

        if (path.StartsWith("/report_", StringComparison.OrdinalIgnoreCase)
            && path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
        {
            var sessionKey = path["/report_".Length..^".html".Length];
            if (reportGenerator.TryGetReportPath(sessionKey, out var reportPath))
            {
                return Results.File(reportPath, "text/html; charset=utf-8");
            }

            var files = uploadIndex.GetSessionFiles(sessionKey);
            if (files.Count > 0)
            {
                reportGenerator.Generate(sessionKey, null);
                return Results.File(Path.Combine(reportDir, $"report_{sessionKey}.html"), "text/html; charset=utf-8");
            }

            return Results.Content(BuildMissingReportPage(path, sessionKey), "text/html; charset=utf-8", Encoding.UTF8, (int)HttpStatusCode.NotFound);
        }

        return Results.Content(BuildNotFoundPage(path), "text/html; charset=utf-8", Encoding.UTF8, (int)HttpStatusCode.NotFound);
    }
    catch (Exception ex)
    {
        await SafeLogErrorAsync(serverState, path, ex);
        return Results.Text($"error: {ex.Message}", "text/plain", Encoding.UTF8, (int)HttpStatusCode.InternalServerError);
    }
    finally
    {
        AuthRequestContext.Clear();
    }
});

var localIp = GetLocalIPv4();
Console.WriteLine($"UProfiler local server started. (v{VersionInfo.Current})");
Console.WriteLine($"  Local:   http://localhost:{port}/");
if (!string.IsNullOrWhiteSpace(localIp))
{
    Console.WriteLine($"  Network: http://{localIp}:{port}/");
}
Console.WriteLine($"Upload dir:  {uploadDir}");
Console.WriteLine($"Report dir:  {reportDir}");
Console.WriteLine($"Log dir:     {logDir}");
PrintAuthStatus(authSettings, port);
Console.WriteLine("Press Ctrl+C to stop.");

app.Run();

static bool TryGenerateReportOnly(string[] args, string uploadDir, string reportDir, out string reportPath)
{
    reportPath = "";
    if (args.Length < 2 || !args[0].Equals("--generate-report", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var dataRoot = ResolveDataRoot(AppContext.BaseDirectory);
    uploadDir = Path.Combine(dataRoot, "uploads");
    reportDir = Path.Combine(dataRoot, "reports");
    Directory.CreateDirectory(reportDir);

    var sessionKey = args[1];
    var packageName = args.Length > 2 ? args[2] : null;
    var uploadIndex = new UploadIndex(uploadDir);
    var reportGenerator = new ReportGenerator(uploadIndex, reportDir);
    reportPath = reportGenerator.Generate(sessionKey, packageName);
    return true;
}

static string ResolveDataRoot(string baseDir)
{
    var candidates = new[]
    {
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "Release", "net8.0")),
        Path.GetFullPath(Path.Combine(baseDir, "..", "Release", "net8.0")),
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "bin", "Release", "net8.0")),
        baseDir
    };

    return candidates
        .Where(dir => Directory.Exists(Path.Combine(dir, "uploads")))
        .OrderByDescending(dir => Directory.GetDirectories(Path.Combine(dir, "uploads")).Length)
        .FirstOrDefault() ?? baseDir;
}

static IResult TryServeCaptureImage(string path, UploadIndex uploadIndex)
{
    // /capture/{sessionKey}/{frameIndex}.png
    var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 3
        || !parts[0].Equals("capture", StringComparison.OrdinalIgnoreCase)
        || !parts[2].EndsWith(".png", StringComparison.OrdinalIgnoreCase))
    {
        return Results.NotFound();
    }

    var sessionKey = parts[1];
    var frameToken = Path.GetFileNameWithoutExtension(parts[2]);
    if (!int.TryParse(frameToken, out var frameIndex))
    {
        return Results.NotFound();
    }

    if (!CaptureFrameService.TryResolveImagePath(sessionKey, frameIndex, uploadIndex, out var imagePath))
    {
        return Results.NotFound();
    }

    return Results.File(imagePath, "image/png");
}

static bool TryServeStaticFile(string path, string webRoot, out IResult result)
{
    result = Results.NotFound();
    if (!path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase)
        && !path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var relativePath = path.Split('?', 2)[0].TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
    var fullPath = Path.GetFullPath(Path.Combine(webRoot, relativePath));
    var normalizedRoot = Path.GetFullPath(webRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
    if (!fullPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
    {
        return true;
    }

    result = Results.File(fullPath, GetContentType(fullPath));
    return true;
}

static string GetContentType(string filePath)
{
    return Path.GetExtension(filePath).ToLowerInvariant() switch
    {
        ".css" => "text/css; charset=utf-8",
        ".js" => "application/javascript; charset=utf-8",
        ".map" => "application/json; charset=utf-8",
        _ => "application/octet-stream"
    };
}

static string ResolveStaticWebRoot(string baseDir)
{
    var candidates = new[]
    {
        Path.Combine(baseDir, "wwwroot"),
        Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "wwwroot")),
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "wwwroot"))
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(Path.Combine(candidate, "css", "portal.css")))
        {
            return candidate;
        }
    }

    throw new DirectoryNotFoundException(
        "未找到 wwwroot 静态资源目录，请重新编译 UProfiler-Server 项目。");
}

static int ResolvePort(string[] args)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] is "--port" or "-p" && int.TryParse(args[i + 1], out var port))
        {
            return port;
        }
    }

    var envPort = Environment.GetEnvironmentVariable("MONITOR_TOOL_PORT");
    if (int.TryParse(envPort, out var envValue))
    {
        return envValue;
    }

    return 8080;
}

static int FindAvailablePort(int startPort)
{
    for (var port = startPort; port <= startPort + 10; port++)
    {
        if (IsPortAvailable(port))
        {
            return port;
        }
    }

    throw new InvalidOperationException($"No available port found between {startPort} and {startPort + 10}.");
}

static bool IsPortAvailable(int port)
{
    try
    {
        using var listener = new TcpListener(IPAddress.Loopback, port);
        listener.Start();
        listener.Stop();
        return true;
    }
    catch (SocketException)
    {
        return false;
    }
}

static string? GetLocalIPv4()
{
    try
    {
        return Dns.GetHostAddresses(Dns.GetHostName())
            .FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork
                && !IPAddress.IsLoopback(address))
            ?.ToString();
    }
    catch
    {
        return null;
    }
}

static async Task SafeLogRequestAsync(ServerState state, HttpContext context, string path, IFormCollection? form)
{
    try
    {
        await LogRequestAsync(state, context, path, form);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Log write failed: {ex.Message}");
    }
}

static async Task SafeLogErrorAsync(ServerState state, string path, Exception ex)
{
    try
    {
        await LogErrorAsync(state, path, ex);
    }
    catch (Exception logEx)
    {
        Console.WriteLine($"Log write failed: {logEx.Message}");
        Console.WriteLine(ex);
    }
}

static async Task AppendLogAsync(ServerState state, string message)
{
    var logFile = Path.Combine(AppContext.BaseDirectory, "logs", $"{DateTime.Now:yyyy-MM-dd}.log");
    await state.LogWriteLock.WaitAsync();
    try
    {
        await using var stream = new FileStream(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        await writer.WriteAsync(message);
    }
    finally
    {
        state.LogWriteLock.Release();
    }
}

static async Task LogRequestAsync(ServerState state, HttpContext context, string path, IFormCollection? form)
{
    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
    var sb = new StringBuilder();
    sb.AppendLine($"[{timestamp}] {context.Request.Method} {path}{context.Request.QueryString}");

    foreach (var header in context.Request.Headers)
    {
        sb.AppendLine($"  Header: {header.Key} = {header.Value}");
    }

    if (form != null)
    {
        foreach (var field in form)
        {
            if (form.Files.All(file => file.Name != field.Key))
            {
                sb.AppendLine($"  Form: {field.Key} = {field.Value}");
            }
        }

        foreach (var file in form.Files)
        {
            sb.AppendLine($"  File: {file.Name} = {file.FileName} ({file.Length} bytes)");
        }
    }

    sb.AppendLine();
    var text = sb.ToString();
    await AppendLogAsync(state, text);
    Console.WriteLine(text.TrimEnd());
}

static async Task LogErrorAsync(ServerState state, string path, Exception ex)
{
    var message = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] ERROR {path}{Environment.NewLine}{ex}{Environment.NewLine}";
    await AppendLogAsync(state, message);
    Console.WriteLine(message);
}

static async Task SaveUploadAsync(ServerState state, UploadIndex uploadIndex, ProjectCatalog projectCatalog, HttpContext context, IFormCollection? form)
{
    if (form == null)
    {
        return;
    }

    foreach (var file in form.Files)
    {
        var safeName = ResolveUploadFileName(form, file);
        var sessionKey = UploadIndex.ExtractSessionKey(safeName) ?? "unknown";
        var sessionDir = uploadIndex.GetSessionDirectory(sessionKey);
        Directory.CreateDirectory(sessionDir);
        var targetPath = Path.Combine(sessionDir, safeName);

        var sessionLock = state.GetSessionUploadLock(sessionKey);
        await sessionLock.WaitAsync();
        try
        {
            var tempPath = targetPath + ".uploading";
            await using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await file.CopyToAsync(stream);
            }

            File.Move(tempPath, targetPath, overwrite: true);
            uploadIndex.Register(safeName, targetPath);
            projectCatalog.InvalidateCache();
            Console.WriteLine($"Saved upload: {targetPath} ({file.Length} bytes)");
        }
        finally
        {
            sessionLock.Release();
        }
    }
}

static string ResolveUploadFileName(IFormCollection form, IFormFile file)
{
    var fileNameField = form["fileName"].ToString();
    if (!string.IsNullOrWhiteSpace(fileNameField))
    {
        return Path.GetFileName(fileNameField);
    }

    var multipartName = Path.GetFileName(file.FileName);
    if (!string.IsNullOrWhiteSpace(multipartName)
        && !string.Equals(multipartName, "folder.dat", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(multipartName, "data.bin", StringComparison.OrdinalIgnoreCase))
    {
        return multipartName;
    }

    return $"{file.Name}.bin";
}

static string BuildMissingReportPage(string path, string sessionKey)
{
    return $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head><meta charset="utf-8" /><title>报告不存�?/title>{{PortalHtmlBuilder.GetTopNavStyles()}}</head>
<body>
{{PortalHtmlBuilder.BuildTopNav(PortalHtmlBuilder.NavTab.Report, null)}}
<div style="font-family:Segoe UI,Arial,sans-serif;margin:40px;">
  <h2>报告尚未生成</h2>
  <p>路径�?code>{{WebUtility.HtmlEncode(path)}}</code></p>
  <p>会话 ID�?code>{{WebUtility.HtmlEncode(sessionKey)}}</code></p>
  <p>请先�?Unity 中完成一次监控并停止，确保数据已上传到本服务器�?/p>
  <p><a href="/">返回首页</a></p>
</div>
</body>
</html>
""";
}

static void PrintAuthStatus(AuthSettings settings, int port)
{
    if (!settings.Enabled)
    {
        Console.WriteLine("Auth:        disabled (copy auth.example.json to auth.json to enable)");
        return;
    }

    Console.WriteLine("Auth:        enabled");
    Console.WriteLine($"  View auth:   {settings.RequireAuthForView}");
    Console.WriteLine($"  Upload auth: {settings.RequireAuthForUpload}");
    if (AuthSettingsLoader.IsFeishuConfigured(settings))
    {
        Console.WriteLine($"  Feishu:      configured, callback {settings.Feishu.RedirectUri}");
    }
    else
    {
        Console.WriteLine("  Feishu:      NOT configured (set appId/appSecret in auth.json)");
    }

    if (settings.SessionSecret.Contains("change-me", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("  Warning:     sessionSecret is default, change it before production deploy");
    }
}

static bool TryHandleAuthRoutes(
    HttpContext context,
    string path,
    string method,
    IFormCollection? form,
    AuthSettings authSettings,
    AuthSessionService authSessionService,
    FeishuOAuthService feishuOAuthService,
    UserStore userStore,
    out IResult result)
{
    result = Results.NotFound();

    if (path.Equals("/login", StringComparison.OrdinalIgnoreCase)
        && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
    {
        var currentUser = AuthRequestContext.Current;
        if (currentUser != null)
        {
            result = Results.Redirect("/");
            return true;
        }

        var error = context.Request.Query["error"].ToString();
        var returnUrl = context.Request.Query["returnUrl"].ToString();
        result = Results.Content(
            AccountHtmlBuilder.BuildLoginPage(
                authSettings,
                string.IsNullOrWhiteSpace(error) ? null : error,
                string.IsNullOrWhiteSpace(returnUrl) ? null : returnUrl),
            "text/html; charset=utf-8",
            Encoding.UTF8);
        return true;
    }

    if (path.Equals("/auth/feishu", StringComparison.OrdinalIgnoreCase)
        && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
    {
        if (!AuthSettingsLoader.IsFeishuConfigured(authSettings))
        {
            result = Results.Redirect("/login?error=" + Uri.EscapeDataString("飞书登录未配置"));
            return true;
        }

        var state = authSessionService.CreateOAuthState();
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = TimeSpan.FromMinutes(10)
        };
        context.Response.Cookies.Append(AuthSessionService.StateCookieName, state, cookieOptions);

        var returnUrl = context.Request.Query["returnUrl"].ToString();
        if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/'))
        {
            context.Response.Cookies.Append("uprofiler_return_url", returnUrl, cookieOptions);
        }

        result = Results.Redirect(feishuOAuthService.BuildAuthorizeUrl(state));
        return true;
    }

    if (path.Equals("/auth/feishu/callback", StringComparison.OrdinalIgnoreCase)
        && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
    {
        var code = context.Request.Query["code"].ToString();
        var state = context.Request.Query["state"].ToString();
        context.Request.Cookies.TryGetValue(AuthSessionService.StateCookieName, out var savedState);
        context.Response.Cookies.Delete(AuthSessionService.StateCookieName);
        context.Request.Cookies.TryGetValue("uprofiler_return_url", out var savedReturnUrl);
        context.Response.Cookies.Delete("uprofiler_return_url");

        if (string.IsNullOrWhiteSpace(code)
            || string.IsNullOrWhiteSpace(state)
            || string.IsNullOrWhiteSpace(savedState)
            || !string.Equals(state, savedState, StringComparison.Ordinal))
        {
            result = Results.Redirect("/login?error=" + Uri.EscapeDataString("飞书授权失败，请重试"));
            return true;
        }

        var feishuUser = feishuOAuthService.ExchangeCodeAsync(code).GetAwaiter().GetResult();
        if (feishuUser == null || string.IsNullOrWhiteSpace(feishuUser.OpenId))
        {
            result = Results.Redirect("/login?error=" + Uri.EscapeDataString("无法获取飞书用户信息"));
            return true;
        }

        var user = userStore.UpsertFromFeishu(
            feishuUser.OpenId,
            feishuUser.UnionId,
            feishuUser.Name,
            feishuUser.AvatarUrl,
            authSettings.AdminOpenIds);

        context.Response.Cookies.Append(
            AuthSessionService.CookieName,
            authSessionService.CreateSessionCookie(user),
            authSessionService.BuildSessionCookieOptions());

        var returnUrl = savedReturnUrl;
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/'))
        {
            returnUrl = "/account/profile";
        }

        result = Results.Redirect(returnUrl);
        return true;
    }

    if (path.Equals("/auth/logout", StringComparison.OrdinalIgnoreCase)
        && method.Equals("POST", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Cookies.Delete(AuthSessionService.CookieName, authSessionService.BuildClearCookieOptions());
        result = Results.Redirect("/login");
        return true;
    }

    if (path.Equals("/account", StringComparison.OrdinalIgnoreCase)
        && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
    {
        result = Results.Redirect("/account/profile");
        return true;
    }

    if (path.Equals("/account/profile", StringComparison.OrdinalIgnoreCase)
        && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
    {
        var user = AuthRequestContext.Current;
        if (user == null)
        {
            result = Results.Redirect("/login?returnUrl=" + Uri.EscapeDataString(path));
            return true;
        }

        result = Results.Content(
            AccountHtmlBuilder.BuildAccountPage(user, AccountHtmlBuilder.AccountSection.Profile),
            "text/html; charset=utf-8",
            Encoding.UTF8);
        return true;
    }

    if (path.Equals("/account/settings", StringComparison.OrdinalIgnoreCase)
        && method.Equals("GET", StringComparison.OrdinalIgnoreCase))
    {
        var user = AuthRequestContext.Current;
        if (user == null)
        {
            result = Results.Redirect("/login?returnUrl=" + Uri.EscapeDataString(path));
            return true;
        }

        result = Results.Content(
            AccountHtmlBuilder.BuildAccountPage(user, AccountHtmlBuilder.AccountSection.Account),
            "text/html; charset=utf-8",
            Encoding.UTF8);
        return true;
    }

    if (path.Equals("/api/account/profile", StringComparison.OrdinalIgnoreCase)
        && method.Equals("POST", StringComparison.OrdinalIgnoreCase))
    {
        var user = AuthRequestContext.Current;
        if (user == null)
        {
            result = Results.Json(new { ok = false, message = "未登录" }, statusCode: (int)HttpStatusCode.Unauthorized);
            return true;
        }

        var update = new UserProfileUpdate
        {
            Username = form?["username"].ToString(),
            Company = form?["company"].ToString(),
            CompanyPhone = form?["companyPhone"].ToString(),
            Occupation = form?["occupation"].ToString(),
            Education = form?["education"].ToString(),
            Major = form?["major"].ToString(),
            Bio = form?["bio"].ToString(),
            BirthYear = TryParseNullableInt(form?["birthYear"].ToString()),
            BirthMonth = TryParseNullableInt(form?["birthMonth"].ToString()),
            BirthDay = TryParseNullableInt(form?["birthDay"].ToString())
        };

        if (string.IsNullOrWhiteSpace(update.Username))
        {
            result = Results.Json(new { ok = false, message = "用户名不能为空" });
            return true;
        }

        var saved = userStore.UpdateProfile(user.Id, update);
        result = Results.Json(new { ok = saved, message = saved ? "已保存" : "保存失败" });
        return true;
    }

    return false;
}

static int? TryParseNullableInt(string? value)
{
    return int.TryParse(value, out var number) && number > 0 ? number : null;
}

static string BuildNotFoundPage(string path)
{
    return $$"""
<!DOCTYPE html>
<html lang="zh-CN">
<head><meta charset="utf-8" /><title>404</title>{{PortalHtmlBuilder.GetTopNavStyles()}}</head>
<body>
{{PortalHtmlBuilder.BuildTopNav(PortalHtmlBuilder.NavTab.Home, null)}}
<div style="font-family:Segoe UI,Arial,sans-serif;margin:40px;">
  <h2>页面不存�?/h2>
  <p>路径�?code>{{WebUtility.HtmlEncode(path)}}</code></p>
  <p><a href="/">返回首页</a></p>
</div>
</body>
</html>
""";
}

sealed class ServerState
{
    readonly SemaphoreSlim logWriteLock = new(1, 1);
    readonly System.Collections.Concurrent.ConcurrentDictionary<string, SemaphoreSlim> sessionUploadLocks = new(StringComparer.OrdinalIgnoreCase);

    public SemaphoreSlim LogWriteLock => logWriteLock;

    public SemaphoreSlim GetSessionUploadLock(string sessionKey) =>
        sessionUploadLocks.GetOrAdd(sessionKey, _ => new SemaphoreSlim(1, 1));
}
