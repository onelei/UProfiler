using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using LemonFramework.UProfiler.Components;
using LemonFramework.UProfiler.Core;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

namespace LemonFramework.UProfiler.Demo
{
    /// <summary>
    /// Short play-mode workload for the open scene.
    /// Covers UI, scene rendering, physics, animation, particles, loading,
    /// logs, Lua memory, resource events, and custom report sections.
    /// </summary>
    public class PerformanceDemo : MonoBehaviour
    {
        const float DurationSeconds = 8f;

        UProfilerHost _host;
        bool _running;
        bool _stopped;
        float _startTime;
        float _nextMetricTime;
        float _uiMs;
        float _sceneMs;
        float _loadMs;
        int _uiFrames;
        int _sceneFrames;
        int _loadFrames;
        int _uiStartFrame;
        int _sceneStartFrame;
        int _loadStartFrame;
        string _phase = "准备";

        Font _font;
        Text _statusText;
        Text _reportText;
        readonly List<Text> _labels = new List<Text>();
        readonly List<GameObject> _meshes = new List<GameObject>();
        readonly List<UnityEngine.Object> _keepAlive = new List<UnityEngine.Object>();
        ParticleSystem _particles;
        bool _loggedWarning;
        bool _loggedError;
        bool _loggedException;
        bool _loggedAssert;
        bool _loadedResources;
        bool _luaDetailSent;
        bool _toggledLabel;
        TextAsset _loadedText;

        void Awake()
        {
            _host = FindObjectOfType<UProfilerHost>();
            if (_host == null)
            {
                var go = new GameObject("UProfilerHost");
                _host = go.AddComponent<UProfilerHost>();
            }

            _host.intervalFrame = 30;
            _host.ignoreFrameCount = 5;
            _host.enableLog = true;
            _host.enableFrameTexture = true;
            _host.enableResMemoryDistributionInfo = true;
            _host.enableModuleTime = true;
            _host.enableHardwareInfo = true;
            _host.enableExtendedReports = true;
            _host.enableLuaMemory = true;
            _host.enableResourceManagement = true;
#if UNITY_2020_1_OR_NEWER
            _host.enableRenderInfo = true;
#endif
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            _host.enableFunctionAnalysis = true;
#endif
            LuaMemoryProvider.SnapshotHandler = BuildLuaSnapshot;
        }

        IEnumerator Start()
        {
            yield return null;
            _host.BeginRecord();
            BuildUi();
            BuildScene();
            if (_host.UploadTips == null)
            {
                _host.UploadTips = _statusText;
            }

            if (_host.ReportUrl == null)
            {
                _host.ReportUrl = _reportText;
            }
            _running = true;
            _startTime = Time.unscaledTime;
            _nextMetricTime = _startTime;
            _uiStartFrame = Time.frameCount;
            _phase = "UI";
            Debug.Log("[PerfDemo] 开始采集。将覆盖界面、场景、资源、日志和自定义指标。");
        }

        void Update()
        {
            if (!_running || _stopped)
            {
                return;
            }

            var elapsed = Time.unscaledTime - _startTime;
            TickUi();
            if (elapsed < 2.5f)
            {
                _phase = "UI";
            }
            else if (elapsed < 5.5f)
            {
                if (_phase != "场景")
                {
                    CustomDataTracker.RecordCodeSegment("UI压测", _uiStartFrame, Time.frameCount, _uiMs);
                    _phase = "场景";
                    _sceneStartFrame = Time.frameCount;
                    Debug.Log("[PerfDemo] 进入场景压测：网格、物理、动画、粒子。");
                }

                TickScene();
            }
            else
            {
                if (_phase != "加载")
                {
                    CustomDataTracker.RecordCodeSegment("场景压测", _sceneStartFrame, Time.frameCount, _sceneMs);
                    _phase = "加载";
                    _loadStartFrame = Time.frameCount;
                    Debug.LogWarning("[PerfDemo] 粒子与网格数量上升，进入资源加载阶段。");
                }

                TickLoad();
            }

            EmitScheduledLogs(elapsed);
            if (Time.unscaledTime >= _nextMetricTime)
            {
                _nextMetricTime = Time.unscaledTime + 0.5f;
                RecordCustomMetrics();
            }

            if (_statusText != null)
            {
                _statusText.text = $"性能演示 {_phase}  {elapsed:0.0}/{DurationSeconds:0.0}s";
            }

            if (elapsed >= DurationSeconds)
            {
                Finish();
            }
        }

        [FunctionAnalysis]
        void TickUi()
        {
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < _labels.Count; i++)
            {
                _labels[i].text = $"条目 {i:00}  帧 {Time.frameCount % 1000}";
            }

            if (!_toggledLabel && _labels.Count > 0 && Time.frameCount > _uiStartFrame + 20)
            {
                _toggledLabel = true;
                _labels[0].gameObject.SetActive(false);
                ResourceEventTracker.SetCurrentFrame(Time.frameCount);
                ResourceEventTracker.RecordActivate("UI/PerfDemo/Label0", 0.2, false);
            }

            watch.Stop();
            _uiMs += (float)watch.Elapsed.TotalMilliseconds;
            _uiFrames++;
        }

        [FunctionAnalysis]
        void TickScene()
        {
            var watch = Stopwatch.StartNew();
            var spin = 25f * Time.deltaTime;
            for (var i = 0; i < _meshes.Count; i++)
            {
                if (_meshes[i] != null)
                {
                    _meshes[i].transform.Rotate(0f, spin, 0f, Space.World);
                }
            }

            if (_particles != null)
            {
                _particles.Emit(2);
            }

            watch.Stop();
            _sceneMs += (float)watch.Elapsed.TotalMilliseconds;
            _sceneFrames++;
        }

        [FunctionAnalysis]
        void TickLoad()
        {
            var watch = Stopwatch.StartNew();
            if (!_loadedResources)
            {
                _loadedResources = true;
                LoadDemoAssets();
            }

            watch.Stop();
            _loadMs += (float)watch.Elapsed.TotalMilliseconds;
            _loadFrames++;
        }

        void LoadDemoAssets()
        {
            ResourceEventTracker.SetCurrentFrame(Time.frameCount);
            var watch = Stopwatch.StartNew();
            _loadedText = Resources.Load<TextAsset>("UProfilerDemoNote");
            watch.Stop();
            ResourceEventTracker.RecordResourcesLoad(
                "Resources/UProfilerDemoNote",
                watch.Elapsed.TotalMilliseconds);
            if (_loadedText != null)
            {
                _keepAlive.Add(_loadedText);
                Debug.Log($"[PerfDemo] Resources.Load 文本 { _loadedText.text.Length } 字符，耗时 {watch.Elapsed.TotalMilliseconds:0.00} ms。");
            }
            else
            {
                Debug.LogError("[PerfDemo] Resources.Load 未找到 UProfilerDemoNote。");
            }

            var shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Diffuse");
            }

            if (shader != null)
            {
                _keepAlive.Add(shader);
            }

            var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = "PerfDemoTexture"
            };
            texture.SetPixel(0, 0, Color.cyan);
            texture.Apply(false, false);
            _keepAlive.Add(texture);

            if (shader != null)
            {
                var material = new Material(shader) { name = "PerfDemoMaterial" };
                material.mainTexture = texture;
                _keepAlive.Add(material);
            }

            var mesh = new Mesh { name = "PerfDemoMesh" };
            mesh.vertices = new[]
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 1, 0)
            };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateNormals();
            _keepAlive.Add(mesh);

            var clip = AudioClip.Create("PerfDemoTone", 4410, 1, 44100, false);
            clip.SetData(new float[4410], 0);
            _keepAlive.Add(clip);
            var source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.playOnAwake = false;
            source.volume = 0.05f;
            source.Play();

            var asset = ScriptableObject.CreateInstance<PerformanceDemoAsset>();
            asset.name = "PerfDemoAsset";
            asset.title = "performance-demo";
            _keepAlive.Add(asset);

            ResourceEventTracker.RecordAssetBundleLoad("Demo/PerfDemoBundle.bundle", 1.25);
            ResourceEventTracker.RecordInstantiate("Demo/PerfDemoCube.prefab", 0.35);

            if (_loadedText != null)
            {
                var unloadWatch = Stopwatch.StartNew();
                Resources.UnloadAsset(_loadedText);
                unloadWatch.Stop();
                ResourceEventTracker.RecordResourcesUnload(
                    "Resources/UProfilerDemoNote",
                    unloadWatch.Elapsed.TotalMilliseconds);
                _loadedText = null;
            }

            ResourceEventTracker.RecordAssetBundleUnload("Demo/PerfDemoBundle.bundle", 0.4);

            if (_meshes.Count > 4)
            {
                var victim = _meshes[_meshes.Count - 1];
                _meshes.RemoveAt(_meshes.Count - 1);
                ResourceEventTracker.RecordDestroy(victim.name, 0.15);
                Destroy(victim);
            }

            Debug.Log("[PerfDemo] 已加载贴图、材质、网格、音频、ScriptableObject，并记录资源事件。");
        }

        void EmitScheduledLogs(float elapsed)
        {
            if (!_loggedWarning && elapsed >= 1f)
            {
                _loggedWarning = true;
                Debug.LogWarning("[PerfDemo] UI 文本每帧刷新，Canvas 重建次数偏高。");
            }

            if (!_loggedError && elapsed >= 2f)
            {
                _loggedError = true;
                Debug.LogError("[PerfDemo] 演示错误：可选贴图 Demo/OptionalSprite 不存在。");
            }

            if (!_loggedException && elapsed >= 3.2f)
            {
                _loggedException = true;
                try
                {
                    throw new InvalidOperationException("[PerfDemo] 演示异常：模拟资源句柄为空。");
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            if (!_loggedAssert && elapsed >= 4.2f)
            {
                _loggedAssert = true;
                Debug.LogAssertion("[PerfDemo] 演示断言：检查可选配置未通过。");
            }
        }

        void RecordCustomMetrics()
        {
            var frame = Time.frameCount;
            CustomDataTracker.RecordDashboardMetric("性能演示", "UI文本数", "个", frame, _labels.Count);
            CustomDataTracker.RecordDashboardMetric("性能演示", "场景网格数", "个", frame, _meshes.Count);
            CustomDataTracker.RecordDashboardMetric("性能演示", "粒子数", "个", frame, _particles != null ? _particles.particleCount : 0);
            CustomDataTracker.RecordVar(frame, "phase", _phase);
            CustomDataTracker.RecordVar(frame, "uiCount", _labels.Count.ToString());
            CustomDataTracker.RecordVar(frame, "meshCount", _meshes.Count.ToString());
        }

        void Finish()
        {
            _stopped = true;
            _running = false;
            if (_phase == "加载")
            {
                CustomDataTracker.RecordCodeSegment("资源加载", _loadStartFrame, Time.frameCount, _loadMs);
            }

            CustomDataTracker.RecordFuncGroup("演示逻辑", MakeFuncRow("PerformanceDemo.TickUi", _uiMs, _uiFrames));
            CustomDataTracker.RecordFuncGroup("演示逻辑", MakeFuncRow("PerformanceDemo.TickScene", _sceneMs, _sceneFrames));
            CustomDataTracker.RecordFuncGroup("演示逻辑", MakeFuncRow("PerformanceDemo.TickLoad", _loadMs, _loadFrames));
            Debug.Log($"[PerfDemo] 采集结束，开始写出报告。目录：{ShareDatas.RecordDirectory}");
            if (_statusText != null)
            {
                _statusText.text = "采集结束，正在上传报告";
            }

            if (_reportText != null && _host.ReportUrl != _reportText)
            {
                _reportText.text = ShareDatas.RecordDirectory;
            }

            _host.EndRecord();
        }

        static ModuleFuncStackFunctionRow MakeFuncRow(string name, float totalMs, int frames)
        {
            var avg = frames > 0 ? totalMs / frames : 0;
            return new ModuleFuncStackFunctionRow
            {
                name = name,
                avgMs = Math.Round(avg, 2),
                totalMs = Math.Round(totalMs, 2),
                selfMs = Math.Round(avg, 2),
                totalPct = 0,
                selfPct = 0,
                callCount = frames,
                callsPerFrame = 1,
                frameCount = Math.Max(1, frames)
            };
        }

        LuaMemoryUploadData BuildLuaSnapshot(int frameIndex)
        {
            var data = new LuaMemoryUploadData();
            AddCurve(data, "LUA堆内存", "KB", frameIndex, 1800 + frameIndex * 1.6);
            AddCurve(data, "Table数量", "个", frameIndex, 240 + frameIndex * 0.15);
            AddCurve(data, "Function数量", "个", frameIndex, 80 + frameIndex * 0.04);
            AddCurve(data, "Userdata数量", "个", frameIndex, 16 + frameIndex * 0.02);
            if (_luaDetailSent)
            {
                return data;
            }

            _luaDetailSent = true;
            data.allocations.Add(new LuaHeapAllocationRow
            {
                type = "table",
                sizeBytes = 65536,
                count = 128,
                functionName = "PerfDemo.BuildUi",
                avgAlloc = 512
            });
            data.allocations.Add(new LuaHeapAllocationRow
            {
                type = "string",
                sizeBytes = 24576,
                count = 64,
                functionName = "PerfDemo.TickUi",
                avgAlloc = 384
            });
            data.monoRefs.Add(new LuaMonoRefRow
            {
                objectName = "PerfDemoPanel",
                refCount = 6,
                destroyedCount = 1
            });
            data.aiDiagnosis.Add(new ModuleFuncStackAiRow
            {
                title = "演示 Lua 堆随帧增长",
                severity = "Low",
                suggestion = "当前工程没有 xLua/SLua，这组数据由演示脚本写入。"
            });
            return data;
        }

        static void AddCurve(LuaMemoryUploadData data, string label, string unit, int frame, double value)
        {
            data.curves.Add(new LuaMemoryCurveRow
            {
                label = label,
                unit = unit,
                frames = new List<int> { frame },
                values = new List<double> { Math.Round(value, 2) }
            });
        }

        void BuildUi()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null)
            {
                _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            if (_font != null)
            {
                _keepAlive.Add(_font);
            }

            var canvasGo = new GameObject("PerfDemoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var panel = CreateUiObject("Panel", canvasGo.transform);
            var panelImage = panel.AddComponent<Image>();
            panelImage.color = new Color(0f, 0f, 0f, 0.45f);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-24f, -24f);
            panelRect.sizeDelta = new Vector2(420f, 640f);

            _statusText = CreateLabel(panel.transform, "Status", "性能演示准备中", 22, FontStyle.Bold);
            var statusRect = _statusText.rectTransform;
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(1f, 1f);
            statusRect.pivot = new Vector2(0.5f, 1f);
            statusRect.anchoredPosition = new Vector2(0f, -12f);
            statusRect.sizeDelta = new Vector2(-24f, 36f);

            _reportText = CreateLabel(panel.transform, "ReportUrl", "", 16, FontStyle.Normal);
            var reportRect = _reportText.rectTransform;
            reportRect.anchorMin = new Vector2(0f, 0f);
            reportRect.anchorMax = new Vector2(1f, 0f);
            reportRect.pivot = new Vector2(0.5f, 0f);
            reportRect.anchoredPosition = new Vector2(0f, 12f);
            reportRect.sizeDelta = new Vector2(-24f, 48f);
            _reportText.color = new Color(0.6f, 0.85f, 1f);

            for (var i = 0; i < 12; i++)
            {
                var label = CreateLabel(panel.transform, "Label" + i, "条目 " + i, 18, FontStyle.Normal);
                var rect = label.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchoredPosition = new Vector2(0f, -56f - i * 36f);
                rect.sizeDelta = new Vector2(-32f, 32f);
                _labels.Add(label);
            }

            var buttonGo = CreateUiObject("RefreshButton", panel.transform);
            var buttonImage = buttonGo.AddComponent<Image>();
            buttonImage.color = new Color(0.2f, 0.45f, 0.85f, 1f);
            buttonGo.AddComponent<Button>();
            var buttonRect = buttonGo.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 0f);
            buttonRect.anchorMax = new Vector2(1f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 68f);
            buttonRect.sizeDelta = new Vector2(-32f, 36f);
            var buttonLabel = CreateLabel(buttonGo.transform, "Text", "刷新列表", 18, FontStyle.Bold);
            Stretch(buttonLabel.rectTransform);
        }

        void BuildScene()
        {
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "PerfDemoGround";
            ground.transform.position = new Vector3(0f, 0f, 2f);
            ground.transform.localScale = new Vector3(1.2f, 1f, 1.2f);

            var shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            for (var i = 0; i < 12; i++)
            {
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "PerfDemoCube" + i;
                var column = i % 4;
                var row = i / 4;
                cube.transform.position = new Vector3(-3f + column * 2f, 0.5f, row * 2f);
                if (shader != null && i % 3 == 0)
                {
                    var material = new Material(shader) { name = "PerfDemoCubeMat" + i };
                    material.color = Color.HSVToRGB(i / 12f, 0.6f, 0.9f);
                    cube.GetComponent<Renderer>().sharedMaterial = material;
                    _keepAlive.Add(material);
                }

                _meshes.Add(cube);
            }

            for (var i = 0; i < 4; i++)
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
                body.name = "PerfDemoBody" + i;
                body.transform.position = new Vector3(-1.5f + i, 3f + i * 0.4f, 1f);
                body.transform.localScale = Vector3.one * 0.6f;
                body.AddComponent<Rigidbody>();
                _meshes.Add(body);
            }

            var bob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bob.name = "PerfDemoBob";
            bob.transform.position = new Vector3(0f, 1.2f, 4f);
            var animClip = new AnimationClip { name = "PerfDemoBob", legacy = true };
            animClip.SetCurve("", typeof(Transform), "localPosition.y", AnimationCurve.EaseInOut(0f, 0.8f, 1f, 2.2f));
            var animation = bob.AddComponent<Animation>();
            animation.AddClip(animClip, animClip.name);
            animation.clip = animClip;
            animation.wrapMode = WrapMode.Loop;
            animation.Play();
            _keepAlive.Add(animClip);
            _meshes.Add(bob);

            var particleGo = new GameObject("PerfDemoParticles");
            particleGo.transform.position = new Vector3(2.5f, 1f, 3f);
            _particles = particleGo.AddComponent<ParticleSystem>();
            var main = _particles.main;
            main.startLifetime = 1.2f;
            main.startSpeed = 1.5f;
            main.maxParticles = 64;
            _particles.Play();
        }

        Text CreateLabel(Transform parent, string name, string value, int size, FontStyle style)
        {
            var go = CreateUiObject(name, parent);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = size;
            text.fontStyle = style;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.text = value;
            return text;
        }

        static GameObject CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }

    public class PerformanceDemoAsset : ScriptableObject
    {
        public string title;
    }
}
