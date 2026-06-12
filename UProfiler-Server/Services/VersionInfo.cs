namespace UProfiler.Server.Services;

public static class VersionInfo
{
    public static string Current { get; } = Load();

    static string Load()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "VERSION"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "VERSION")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "VERSION"))
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            var text = File.ReadAllText(path).Trim();
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return "1.1.1";
    }
}
