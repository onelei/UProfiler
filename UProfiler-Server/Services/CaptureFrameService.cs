using System.IO.Compression;
using System.Text.RegularExpressions;
using UProfiler.Server.Models;

namespace UProfiler.Server.Services;

public static class CaptureFrameService
{
    static readonly Regex FrameIndexRegex = new(
        @"_(\d+)\.png$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static CaptureFrameManifest Build(string sessionKey, IReadOnlyList<SessionUpload> files)
    {
        var zipFile = files.FirstOrDefault(item =>
            item.Prefix.Equals("captureFrame", StringComparison.OrdinalIgnoreCase)
            && item.OriginalName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase));

        if (zipFile == null || !File.Exists(zipFile.SavedPath))
        {
            return new CaptureFrameManifest { SessionKey = sessionKey };
        }

        var sessionDir = Path.GetDirectoryName(zipFile.SavedPath)!;
        var extractDir = Path.Combine(sessionDir, ".captures");
        EnsureExtracted(zipFile.SavedPath, extractDir);

        var frameImages = new SortedDictionary<int, string>();
        foreach (var pngPath in Directory.EnumerateFiles(extractDir, "*.png", SearchOption.AllDirectories))
        {
            var match = FrameIndexRegex.Match(Path.GetFileName(pngPath));
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var frameIndex))
            {
                continue;
            }

            frameImages[frameIndex] = pngPath;
        }

        return new CaptureFrameManifest
        {
            SessionKey = sessionKey,
            FrameImages = frameImages,
            DeviceModel = null
        };
    }

    public static bool TryResolveImagePath(string sessionKey, int frameIndex, UploadIndex uploadIndex, out string imagePath)
    {
        imagePath = "";
        var files = uploadIndex.GetSessionFiles(sessionKey);
        var manifest = Build(sessionKey, files);
        if (manifest.FrameImages.Count == 0)
        {
            return false;
        }

        if (manifest.FrameImages.TryGetValue(frameIndex, out var exactPath) && !string.IsNullOrEmpty(exactPath))
        {
            imagePath = exactPath;
            return File.Exists(imagePath);
        }

        var nearest = manifest.FrameImages.Keys
            .OrderBy(key => Math.Abs(key - frameIndex))
            .First();
        imagePath = manifest.FrameImages[nearest];
        return File.Exists(imagePath);
    }

    static void EnsureExtracted(string zipPath, string extractDir)
    {
        Directory.CreateDirectory(extractDir);
        if (Directory.EnumerateFiles(extractDir, "*.png", SearchOption.AllDirectories).Any())
        {
            return;
        }

        ZipFile.ExtractToDirectory(zipPath, extractDir, overwriteFiles: true);
    }
}
