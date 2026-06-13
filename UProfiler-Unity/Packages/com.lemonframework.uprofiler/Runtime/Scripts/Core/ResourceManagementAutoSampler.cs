using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>
    /// Detects resource lifecycle changes between interval samples and records resourceManagement_ events.
    /// Complements manual ResourceEventTracker calls for Resources / AssetBundle wrappers.
    /// </summary>
    public sealed class ResourceManagementAutoSampler : IDisposable
    {
        enum ObjectKind
        {
            GameObject,
            AssetBundle,
            Resource
        }

        struct SnapshotRow
        {
            public string Name;
            public string Path;
            public ObjectKind Kind;
            public bool Active;
        }

        readonly Dictionary<int, SnapshotRow> _previous = new Dictionary<int, SnapshotRow>();
        bool _active;
        UnityEngine.Events.UnityAction<Scene, LoadSceneMode> _sceneLoadedHandler;

        public void Begin()
        {
            _previous.Clear();
            _active = true;
            CaptureSnapshot(_previous);
            _sceneLoadedHandler = OnSceneLoaded;
            SceneManager.sceneLoaded += _sceneLoadedHandler;
        }

        public void End(int frameIndex)
        {
            if (_active && frameIndex > 0)
            {
                ResourceEventTracker.SetCurrentFrame(frameIndex);
                DiffAndRecord(frameIndex);
            }

            if (_sceneLoadedHandler != null)
            {
                SceneManager.sceneLoaded -= _sceneLoadedHandler;
                _sceneLoadedHandler = null;
            }

            _active = false;
            _previous.Clear();
        }

        public void Sample(int frameIndex)
        {
            if (!_active || frameIndex <= 0)
            {
                return;
            }

            DiffAndRecord(frameIndex);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!_active)
            {
                return;
            }

            var path = string.IsNullOrEmpty(scene.path) ? scene.name : scene.path;
            ResourceEventTracker.Record($"SceneManager.LoadScene({mode})", path, 0, scene.name);
        }

        void DiffAndRecord(int frameIndex)
        {
            var current = new Dictionary<int, SnapshotRow>();
            CaptureSnapshot(current);

            foreach (var pair in current)
            {
                if (_previous.TryGetValue(pair.Key, out var prev))
                {
                    if (pair.Value.Kind == ObjectKind.GameObject && pair.Value.Active != prev.Active)
                    {
                        ResourceEventTracker.RecordActivate(pair.Value.Path, 0, pair.Value.Active);
                    }

                    continue;
                }

                RecordAdded(pair.Value);
            }

            foreach (var pair in _previous)
            {
                if (current.ContainsKey(pair.Key))
                {
                    continue;
                }

                RecordRemoved(pair.Value);
            }

            _previous.Clear();
            foreach (var pair in current)
            {
                _previous[pair.Key] = pair.Value;
            }
        }

        static void CaptureSnapshot(Dictionary<int, SnapshotRow> target)
        {
            var objects = Resources.FindObjectsOfTypeAll<UnityEngine.Object>();
            for (var i = 0; i < objects.Length; i++)
            {
                var obj = objects[i];
                if (!ShouldTrack(obj))
                {
                    continue;
                }

                if (obj is GameObject go)
                {
                    target[go.GetInstanceID()] = new SnapshotRow
                    {
                        Name = go.name,
                        Path = BuildGameObjectPath(go),
                        Kind = ObjectKind.GameObject,
                        Active = go.activeSelf
                    };
                    continue;
                }

                if (obj is AssetBundle bundle)
                {
                    target[bundle.GetInstanceID()] = new SnapshotRow
                    {
                        Name = bundle.name,
                        Path = bundle.name,
                        Kind = ObjectKind.AssetBundle
                    };
                    continue;
                }

                if (IsResourceAsset(obj))
                {
                    target[obj.GetInstanceID()] = new SnapshotRow
                    {
                        Name = obj.name,
                        Path = $"{obj.GetType().Name}/{obj.name}",
                        Kind = ObjectKind.Resource
                    };
                }
            }
        }

        static bool ShouldTrack(UnityEngine.Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var flags = obj.hideFlags;
            if ((flags & HideFlags.HideAndDontSave) != 0)
            {
                return false;
            }

            if (obj.name.StartsWith("Internal-", StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        static bool IsResourceAsset(UnityEngine.Object obj)
        {
            return obj is Texture
                or Mesh
                or Material
                or Shader
                or AudioClip
                or AnimationClip
                or Font
                or TextAsset
                or ScriptableObject
                or Sprite;
        }

        static string BuildGameObjectPath(GameObject go)
        {
            if (go == null)
            {
                return "unknown";
            }

            var path = go.name;
            var parent = go.transform.parent;
            var depth = 0;
            while (parent != null && depth < 8)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
                depth++;
            }

            return path;
        }

        static void RecordAdded(SnapshotRow row)
        {
            switch (row.Kind)
            {
                case ObjectKind.GameObject:
                    ResourceEventTracker.RecordInstantiate(row.Path, 0);
                    break;
                case ObjectKind.AssetBundle:
                    ResourceEventTracker.RecordAssetBundleLoad(row.Path, 0);
                    break;
                case ObjectKind.Resource:
                    ResourceEventTracker.RecordResourcesLoad(row.Path, 0);
                    break;
            }
        }

        static void RecordRemoved(SnapshotRow row)
        {
            switch (row.Kind)
            {
                case ObjectKind.GameObject:
                    ResourceEventTracker.RecordDestroy(row.Path, 0);
                    break;
                case ObjectKind.AssetBundle:
                    ResourceEventTracker.RecordAssetBundleUnload(row.Path, 0);
                    break;
                case ObjectKind.Resource:
                    ResourceEventTracker.RecordResourcesUnload(row.Path, 0);
                    break;
            }
        }

        public void Dispose()
        {
            End(0);
        }
    }
}
