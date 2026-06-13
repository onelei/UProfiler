using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>
    /// Records resource load/unload/instantiate events for resourceManagement_ upload.
    /// Call from game code or wrappers around Resources / AssetBundle APIs.
    /// </summary>
    public static class ResourceEventTracker
    {
        static readonly List<ResourceManagementEventRow> Events = new List<ResourceManagementEventRow>();
        static int _currentFrame = 1;

        public static void SetCurrentFrame(int frameIndex)
        {
            _currentFrame = Math.Max(1, frameIndex);
        }

        public static void Clear()
        {
            Events.Clear();
            _currentFrame = 1;
        }

        public static void Record(string action, string path, double durationMs, string name = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                path = name ?? "unknown";
            }

            Events.Add(new ResourceManagementEventRow
            {
                frame = _currentFrame,
                action = action,
                name = string.IsNullOrEmpty(name) ? System.IO.Path.GetFileName(path) : name,
                path = path,
                scene = SceneManager.GetActiveScene().name,
                durationMs = Math.Round(durationMs, 2)
            });
        }

        public static void RecordAssetBundleLoad(string path, double durationMs) =>
            Record("AssetBundle.LoadFromFile", path, durationMs);

        public static void RecordAssetBundleUnload(string path, double durationMs) =>
            Record("AssetBundle.Unload", path, durationMs);

        public static void RecordResourcesLoad(string path, double durationMs) =>
            Record("Resources.Load", path, durationMs);

        public static void RecordResourcesUnload(string path, double durationMs) =>
            Record("Resources.UnloadAsset", path, durationMs);

        public static void RecordInstantiate(string path, double durationMs) =>
            Record("Object.Instantiate", path, durationMs);

        public static void RecordActivate(string path, double durationMs, bool active) =>
            Record(active ? "GameObject.SetActive(true)" : "GameObject.SetActive(false)", path, durationMs);

        public static void RecordDestroy(string path, double durationMs) =>
            Record("Object.Destroy", path, durationMs);

        public static ResourceManagementUploadData BuildPayload(int totalFrames)
        {
            var frameCount = Math.Max(1, totalFrames);
            var ab = Events.Where(e => e.action.StartsWith("AssetBundle", StringComparison.OrdinalIgnoreCase)).ToList();
            var res = Events.Where(e => e.action.StartsWith("Resources", StringComparison.OrdinalIgnoreCase)).ToList();
            var inst = Events.Where(e =>
                e.action.StartsWith("Object.", StringComparison.OrdinalIgnoreCase)
                || e.action.StartsWith("GameObject.", StringComparison.OrdinalIgnoreCase)).ToList();

            return new ResourceManagementUploadData
            {
                resourcesLoadPer1k = Per1k(res, "Load", frameCount),
                abLoadPer1k = Per1k(ab, "Load", frameCount),
                instantiatePer1k = Per1k(inst, "Instantiate", frameCount),
                activatePer1k = Per1k(inst, "SetActive", frameCount),
                abLoadTop = BuildTop(ab, "Load"),
                resourceLoadTop = BuildTop(res, "Load"),
                instantiateTop = BuildTop(inst, "Instantiate"),
                unloadTop = BuildTop(ab.Concat(res).ToList(), "Unload"),
                assetBundle = ab,
                resource = res,
                instantiate = inst
            };
        }

        public static bool HasEvents => Events.Count > 0;

        static double Per1k(List<ResourceManagementEventRow> events, string keyword, int frameCount)
        {
            var count = events.Count(e => e.action.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0);
            return Math.Round(count * 1000.0 / frameCount, 2);
        }

        static List<ResourceManagementTopRow> BuildTop(List<ResourceManagementEventRow> events, string keyword)
        {
            var stat = new Dictionary<string, ResourceManagementTopRow>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in events)
            {
                if (e.action.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) < 0)
                {
                    continue;
                }

                if (!stat.TryGetValue(e.path, out var row))
                {
                    row = new ResourceManagementTopRow
                    {
                        name = e.name,
                        path = e.path,
                        loadMode = e.action,
                        count = 0
                    };
                    stat[e.path] = row;
                }

                row.count++;
            }

            return stat.Values.OrderByDescending(item => item.count).Take(10).ToList();
        }
    }
}
