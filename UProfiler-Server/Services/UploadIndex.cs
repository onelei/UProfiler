using System.Text.RegularExpressions;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public sealed class UploadIndex
{
    static readonly Regex SessionKeyRegex = new(
        @"_(\d{4}_\d{2}_\d{2}_\d{2}_\d{2}_\d{2})\.(txt|data|zip|csv)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    readonly Dictionary<string, List<SessionUpload>> sessions = new(StringComparer.OrdinalIgnoreCase);
    readonly string uploadDir;

    public UploadIndex(string uploadDir)
    {
        this.uploadDir = uploadDir;
        RebuildFromDisk();
    }

    public void Register(string originalName, string savedPath)
    {
        var sessionKey = ExtractSessionKey(originalName);
        if (sessionKey == null)
        {
            return;
        }

        var prefix = originalName.Split('_').FirstOrDefault() ?? "unknown";
        lock (sessions)
        {
            if (!sessions.TryGetValue(sessionKey, out var list))
            {
                list = new List<SessionUpload>();
                sessions[sessionKey] = list;
            }

            list.RemoveAll(item => string.Equals(item.OriginalName, originalName, StringComparison.OrdinalIgnoreCase));
            list.Add(new SessionUpload
            {
                OriginalName = originalName,
                SavedPath = savedPath,
                Prefix = prefix
            });
        }
    }

    public IReadOnlyList<SessionUpload> GetSessionFiles(string sessionKey)
    {
        lock (sessions)
        {
            return sessions.TryGetValue(sessionKey, out var list)
                ? list.ToList()
                : Array.Empty<SessionUpload>();
        }
    }

    public IReadOnlyCollection<string> GetAllSessionKeys()
    {
        lock (sessions)
        {
            return sessions.Keys.OrderByDescending(key => key, StringComparer.Ordinal).ToList();
        }
    }

    public void RebuildFromDisk()
    {
        if (!Directory.Exists(uploadDir))
        {
            return;
        }

        lock (sessions)
        {
            sessions.Clear();
        }

        foreach (var sessionDir in Directory.GetDirectories(uploadDir))
        {
            var sessionKey = Path.GetFileName(sessionDir);
            foreach (var file in Directory.GetFiles(sessionDir))
            {
                Register(Path.GetFileName(file), file);
            }
        }

        foreach (var file in Directory.GetFiles(uploadDir))
        {
            var name = Path.GetFileName(file);
            if (name.EndsWith("_meta.json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Register(name, file);
        }
    }

    public static string? ExtractSessionKey(string fileName)
    {
        var match = SessionKeyRegex.Match(fileName);
        return match.Success ? match.Groups[1].Value : null;
    }

    public string GetSessionDirectory(string sessionKey)
    {
        return Path.Combine(uploadDir, sessionKey);
    }
}
