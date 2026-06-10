using LemonFramework.UProfiler.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LemonFramework.UProfiler.Components
{
    public class UProfilerHost : MonoBehaviour
    {
        [Header("Enable log capture")] public bool enableLog = false;
        [Header("Capture frame screenshots")] public bool enableFrameTexture = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [Header("Function analysis (requires IL inject via Hook menu)")]
        public bool enableFunctionAnalysis = false;
#endif

        [Header("Mobile power / battery stats (Android)")]
        public bool enableMobileConsumptionInfo = true;

        [Header("Resource memory distribution")]
        public bool enableResMemoryDistributionInfo = true;
#if UNITY_2020_1_OR_NEWER
        [Header("Render stats (DrawCall, triangles)")]
        public bool enableRenderInfo = true;
#endif
        [Header("Sample interval (frames)")] [Range(10, 1000)]
        public int intervalFrame = 100;

        [Header("Frames to skip after start")] public int ignoreFrameCount = 5;

        [Header("Use binary file format (.data instead of .txt)")]
        public bool useBinary = false;

        public Text UploadTips;
        public Text ReportUrl;
        int _fps = 0;
        int _tickTime = 0;
        string _startTime = "";
        float _accumulator = 0;
        int _frames = 0;
        float _timeLeft;
        float _updateInterval = 0.5f;
        bool btnUProfiler = false;
        string btnMsg;
        int _frameIndex = 0;
        Action<bool> UProfilerCallback;
        UProfilerInfos uprofilerInfos = null;
        FrameRates frameRateInfos = null;
#if UNITY_ANDROID && !UNITY_EDITOR
        MemoryUseDatas memoryUseDatas = null; // Android PSS samples
        DevicePowerConsumeInfos devicePowerConsumeInfos = null;
        UnityAndroidProxy unityAndroidProxy;
#endif
#if UNITY_2020_1_OR_NEWER
        RenderInfos _renderInfos = null;
#endif

        /// <summary>
        /// <summary>Per-frame resource memory samples.</summary>
        RecordResInfos _recordResInfos = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        //
        string funcAnalysisFilePath;
#endif

        string logFilePath;

        //
        string deviceFilePath;

        //
        string captureFilePath;

        //
        string testFilePath;

        //
        string uprofilerFilePath;
        string frameRateFilePath;
#if UNITY_ANDROID && !UNITY_EDITOR
    string pssMemoryUsedFilePath;
    //
    string powerConsumeFilePath;
#endif
        //
        string resMemoryDistributionPath;
#if UNITY_2020_1_OR_NEWER
        //
        string renderFilePath;
#endif
        string sceneInfoFilePath;
        SceneInfoData _sceneInfoData = new SceneInfoData();
        string _activeSceneName = "";
        int _activeSceneStartFrame = 1;
        //
        string fileExt;

#if UNITY_2020_1_OR_NEWER
        private ProfilerRecorder setPassCallRecord;
        private ProfilerRecorder drawCallRecord; // draw calls
        private ProfilerRecorder verticesRecord; //
        private ProfilerRecorder trianglesRecord; //

        //
        private ProfilerRecorder gcRecord; //gc

        //private ProfilerRecorder gcMemoryRecord;//gc
        //private ProfilerRecorder mainThreadTimeRecord;
#endif

        void Awake()
        {
            Application.targetFrameRate = 60;
#if UNITY_2020_1_OR_NEWER
            if (enableRenderInfo)
            {
                setPassCallRecord = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
                drawCallRecord = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count");
                verticesRecord = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Vertices Count");
                trianglesRecord = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
                gcRecord = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Reserved Memory");
            }
#endif
        }

        void UProfilerCallBackFunc(bool res)
        {
            if (res)
            {
                fileExt = useBinary ? ConstString.binaryExt : ConstString.textExt;
                Debug.Log(ConstString.uProfilerActive);
                _frameIndex = 0;
                ShareDatas.StartTime = DateTime.Now; //
                _startTime = ShareDatas.StartTime.ToString("yyyy_MM_dd_HH_mm_ss");
                ShareDatas.StartTimeStr = _startTime;
#if UNITY_EDITOR
                PlayerPrefs.SetString("TestTime", _startTime);
                PlayerPrefs.Save();
#endif
                if (enableFrameTexture)
                {
                    captureFilePath = $"{Application.persistentDataPath}/{ConstString.captureFramePrefix}{_startTime}";
                    FileManager.CreateDir(captureFilePath);
                }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (enableFunctionAnalysis && UProfilerSettings.IsFunctionHookEnabled)
                    funcAnalysisFilePath =
                        $"{Application.persistentDataPath}/{ConstString.funcAnalysisPrefix}{_startTime}{fileExt}";
#endif
                if (enableLog)
                    logFilePath = $"{Application.persistentDataPath}/{ConstString.logPrefix}{_startTime}{fileExt}";
                deviceFilePath = $"{Application.persistentDataPath}/{ConstString.devicePrefix}{_startTime}{fileExt}";
                testFilePath = $"{Application.persistentDataPath}/{ConstString.testPrefix}{_startTime}{fileExt}";
                uprofilerFilePath =
                    $"{Application.persistentDataPath}/{ConstString.uProfilerPrefix}{_startTime}{fileExt}";
                frameRateFilePath =
                    $"{Application.persistentDataPath}/{ConstString.frameRatefix}{_startTime}{fileExt}";
#if UNITY_ANDROID && !UNITY_EDITOR
            if (EnableMobileConsumptionInfo)
                powerConsumeFilePath =
 $"{Application.persistentDataPath}/{ConstString.PowerConsumePrefix}{m_StartTime}{fileExt}";
#endif
                if (enableResMemoryDistributionInfo)
                {
                    resMemoryDistributionPath =
                        $"{Application.persistentDataPath}/{ConstString.resMemoryDistributionPrefix}{_startTime}{fileExt}";
#if UNITY_ANDROID && !UNITY_EDITOR
                pssMemoryUsedFilePath =
 $"{Application.persistentDataPath}/{ConstString.PssMemoryPrefix}{m_StartTime}{fileExt}";
#endif
                }
#if UNITY_2020_1_OR_NEWER
                if (enableRenderInfo)
                    renderFilePath =
                        $"{Application.persistentDataPath}/{ConstString.renderPrefix}{_startTime}{fileExt}";
#endif
                sceneInfoFilePath =
                    $"{Application.persistentDataPath}/{ConstString.sceneInfoPrefix}{_startTime}{fileExt}";
                BeginSceneTracking();
                if (enableLog)
                {
                    LogManager.CreateLogFile(logFilePath, System.IO.FileMode.Append);
                    Application.logMessageReceived += LogManager.LogToFile;
                }

                _tickTime = 0;
                InvokeRepeating("Tick", 1.0f, 1.0f);

                if (ReportUrl != null)
                {
                    ReportUrl.gameObject.SetActive(false);
                }

                StartUProfiler();
            }
            else
            {
                Debug.Log(ConstString.uProfilerStop);
                ShareDatas.EndTime = DateTime.Now;
                //
                UploadTestInfo();
                //
                GetSystemInfo();

                CancelInvoke("Tick");
                _tickTime = 0;

                UProfilerInfosReport();
                FrameRateInfosReport();
                SceneInfoReport();
#if UNITY_2020_1_OR_NEWER
                if (enableRenderInfo)
                    RenderInfosReport();
#endif
                if (enableResMemoryDistributionInfo)
                    ResMemoryReport();
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (enableFunctionAnalysis && UProfilerSettings.IsFunctionHookEnabled)
                    FuncAnalysisReport();
#endif
#if UNITY_ANDROID && !UNITY_EDITOR
            if (EnableMobileConsumptionInfo) //
            {
                MobileConsumptionInfoReport();
                MobilePssMemoryUseReport();
            }
#endif
                if (enableFrameTexture)
                    ZipCaptureFiles();

                if (enableLog)
                {
                    Application.logMessageReceived -= LogManager.LogToFile;
                    LogManager.CloseLogFile();
                }

                if (enableLog)
                {
                    UploadFile(logFilePath);
                }

                HttpGet(string.Format(Config.ReportRecordUpdateRequestUrl, Application.identifier, _startTime),
                    (result) =>
                    {
                        if (result)
                        {
                            if (ReportUrl != null)
                            {
                                ReportUrl.gameObject.SetActive(true);
                                ReportUrl.text =
                                    $"<a href={string.Format(ShareDatas.ReportUrl, ShareDatas.StartTimeStr)}>{string.Format(ShareDatas.ReportUrl, ShareDatas.StartTimeStr)}</a>";
                            }
                        }
                    });
            }
        }

        [FunctionAnalysis]
        void Start()
        {
            btnMsg = ConstString.uProfilerBegin;
            GameObject.DontDestroyOnLoad(gameObject);
            UProfilerCallback += UProfilerCallBackFunc;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            UProfilerCallback -= UProfilerCallBackFunc;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!btnUProfiler || _frameIndex <= ignoreFrameCount)
            {
                return;
            }

            CommitSceneSegment(_frameIndex - ignoreFrameCount);
            _activeSceneName = scene.name;
            _activeSceneStartFrame = _frameIndex - ignoreFrameCount;
        }

        void BeginSceneTracking()
        {
            _sceneInfoData = new SceneInfoData();
            _activeSceneName = SceneManager.GetActiveScene().name;
            _activeSceneStartFrame = 1;
        }

        void CommitSceneSegment(int endFrame)
        {
            if (string.IsNullOrEmpty(_activeSceneName) || endFrame < _activeSceneStartFrame)
            {
                return;
            }

            _sceneInfoData.segments.Add(new SceneSegmentData
            {
                sceneName = _activeSceneName,
                startFrame = _activeSceneStartFrame,
                endFrame = endFrame,
                note = ""
            });
        }

        void FinishSceneTracking()
        {
            if (string.IsNullOrEmpty(_activeSceneName))
            {
                return;
            }

            var endFrame = Math.Max(_activeSceneStartFrame, _frameIndex - ignoreFrameCount);
            CommitSceneSegment(endFrame);
            _activeSceneName = "";
        }

        [FunctionAnalysis]
        public void HttpGet(string url, Action<bool> callback)
        {
            StartCoroutine(InsecureHttpUtil.Get(url, (success, text) =>
            {
                if (success && text != null && text.Equals("success"))
                {
                    callback?.Invoke(true);
                    Debug.Log("HTTP GET report callback succeeded.");
                }
                else
                {
                    Debug.LogError($"HTTP GET report callback failed: {text}");
                    callback?.Invoke(false);
                }
            }));
        }

        void UploadTestInfo()
        {
            TestInfo testInfo = new TestInfo()
            {
                productName = $"{Application.companyName}.{Application.productName}",
                packageName = Application.identifier,
                platform = Application.platform.ToString(),
                version = Application.version,
                testTime = ShareDatas.GetTestTime(),
                intervalFrame = this.intervalFrame
            };
            // Legacy HTML test info export removed; JSON/binary via FileManager.WriteToFile above.
            bool writeRes = false;
            if (!useBinary)
            {
                writeRes = FileManager.WriteToFile(testFilePath, JsonUtility.ToJson(testInfo));
            }
            else
            {
                writeRes = FileManager.WriteBinaryDataToFile(testFilePath, testInfo);
            }

            if (writeRes)
                UploadFile(testFilePath);
        }

        void StartUProfiler()
        {
            uprofilerInfos = new UProfilerInfos();
            frameRateInfos = new FrameRates();
            _renderInfos = new RenderInfos();
#if UNITY_ANDROID && !UNITY_EDITOR
        devicePowerConsumeInfos = new DevicePowerConsumeInfos();
        memoryUseDatas = new MemoryUseDatas();
#endif
            _recordResInfos = new RecordResInfos();
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    void MobileConsumptionInfoReport()
    {
        bool writeRes = false;
        if (!UseBinary)
        {
            writeRes = FileManager.WriteToFile(powerConsumeFilePath, JsonUtility.ToJson(devicePowerConsumeInfos));
        }
        else
        {
            writeRes = FileManager.WriteBinaryDataToFile(powerConsumeFilePath, devicePowerConsumeInfos);
        }
        if (writeRes)
        {
            UploadFile(powerConsumeFilePath);
        }
    }

     void MobilePssMemoryUseReport()
    {
        bool writeRes = false;
        if (!UseBinary)
        {
            writeRes = FileManager.WriteToFile(pssMemoryUsedFilePath, JsonUtility.ToJson(memoryUseDatas));
        }
        else
        {
            writeRes = FileManager.WriteBinaryDataToFile(pssMemoryUsedFilePath, memoryUseDatas);
        }
        if (writeRes)
        {
            UploadFile(pssMemoryUsedFilePath);
        }
    }
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void FuncAnalysisReport()
        {
            HookUtil.MethodAnalysisReport(_startTime);
            if (File.Exists(funcAnalysisFilePath))
            {
                UploadFile(funcAnalysisFilePath);
            }
            else
            {
                Debug.LogError($"Function analysis file missing: {funcAnalysisFilePath}");
            }
        }
#endif

        void ResMemoryReport()
        {
            bool writeRes = false;
            if (!useBinary)
            {
                writeRes = FileManager.WriteToFile(resMemoryDistributionPath, JsonUtility.ToJson(_recordResInfos));
            }
            else
            {
                writeRes = FileManager.WriteBinaryDataToFile(resMemoryDistributionPath, _recordResInfos);
            }

            if (writeRes)
            {
                UploadFile(resMemoryDistributionPath);
            }
        }

#if UNITY_2020_1_OR_NEWER
        void RenderInfosReport()
        {
            bool writeRes = false;
            if (!useBinary)
            {
                writeRes = FileManager.WriteToFile(renderFilePath, JsonUtility.ToJson(_renderInfos));
            }
            else
            {
                writeRes = FileManager.WriteBinaryDataToFile(renderFilePath, _renderInfos);
            }

            if (writeRes)
            {
                UploadFile(renderFilePath);
            }
        }
#endif

        void SceneInfoReport()
        {
            FinishSceneTracking();
            if (_sceneInfoData.segments.Count == 0)
            {
                return;
            }

            var writeRes = FileManager.WriteToFile(sceneInfoFilePath, JsonUtility.ToJson(_sceneInfoData));
            if (writeRes)
            {
                UploadFile(sceneInfoFilePath);
            }
        }

        void FrameRateInfosReport()
        {
            bool writeRes = false;
            if (!useBinary)
            {
                writeRes = FileManager.WriteToFile(frameRateFilePath, JsonUtility.ToJson(frameRateInfos));
            }
            else
            {
                writeRes = FileManager.WriteBinaryDataToFile(frameRateFilePath, frameRateInfos);
            }

            if (writeRes)
            {
                UploadFile(frameRateFilePath);
            }
        }

        void UProfilerInfosReport()
        {
            //if (uprofilerInfos.UProfilerInfoList.Count > 1)
            //{
            //    uprofilerInfos.UProfilerInfoList.RemoveAt(uprofilerInfos.UProfilerInfoList.Count - 1);
            //}
            bool writeRes = false;
            if (!useBinary)
            {
                writeRes = FileManager.WriteToFile(uprofilerFilePath, JsonUtility.ToJson(uprofilerInfos));
            }
            else
            {
                writeRes = FileManager.WriteBinaryDataToFile(uprofilerFilePath, uprofilerInfos);
            }

            if (writeRes)
            {
                UploadFile(uprofilerFilePath);
            }
        }

        void Tick()
        {
            _tickTime++;
        }

        void UploadFile(string filePath)
        {
            if (Config.UseFtpUpload)
            {
                FileFtpUploadManager.UploadFile(filePath, (sender, e) =>
                {
                    Debug.Log("Uploading Progreess : " + e.ProgressPercentage);
                    if (e.ProgressPercentage > 0 && e.ProgressPercentage < 100)
                    {
                        if (UploadTips != null && !UploadTips.gameObject.activeSelf)
                        {
                            UploadTips.gameObject.SetActive(true);
                        }
                    }
                    else if (e.ProgressPercentage >= 100)
                    {
                        if (UploadTips != null && UploadTips.gameObject.activeSelf)
                        {
                            UploadTips.gameObject.SetActive(false);
                        }
                    }

                    UploadTips.text = $"Uploading, progress {e.ProgressPercentage}%";
                }, (sender, e) => { Debug.Log($"File Uploaded :{e.Result}"); });
                return;
            }

            if (UploadTips != null)
            {
                UploadTips.gameObject.SetActive(true);
                UploadTips.text = "Uploading...";
            }

            StartCoroutine(FileHttpUploadManager.UploadFile(filePath, Config.PostFileHeaders, (success, error) =>
            {
                if (UploadTips != null)
                {
                    UploadTips.gameObject.SetActive(false);
                }

                if (success)
                {
                    Debug.Log($"File Uploaded: {filePath}");
                }
                else
                {
                    Debug.LogError($"Upload failed: {error}");
                }
            }));
        }

        [HideAnalysis]
        void OnGUI()
        {
            if (GUI.Button(new Rect(150, 350, 200, 100), btnMsg))
            {
                btnUProfiler = !btnUProfiler;
                btnMsg = btnUProfiler ? ConstString.uProfilerActive : ConstString.uProfilerBegin;
                UProfilerCallback?.Invoke(btnUProfiler);
            }

            if (btnUProfiler)
                btnMsg = $"{ConstString.uProfilerActive}{_tickTime}s";
            GUI.Label(new Rect(Screen.width / 2, 0, 100, 100), "FPS:" + _fps);
        }

        [HideAnalysis]
        void Update()
        {
            _frames++;
            _accumulator += Time.unscaledDeltaTime;
            _timeLeft -= Time.unscaledDeltaTime;
            if (_timeLeft <= 0f)
            {
                _fps = (int) (_accumulator > 0f ? _frames / _accumulator : 0f);
                _frames = 0;
                _accumulator = 0f;
                _timeLeft += _updateInterval;
            }

            if (btnUProfiler)
            {
                ++_frameIndex;
                if (_frameIndex > ignoreFrameCount)
                {
                    var relativeIndex = _frameIndex - ignoreFrameCount;
                    frameRateInfos.frameRateList.Add(new UProfilerFrameInfo()
                    {
                        frameIndex = relativeIndex,
                        frame = _fps
                    });
                    if ((_frameIndex - ignoreFrameCount) % intervalFrame == 0)
                    {
                        var uprofilerInfo = new UProfilerInfo()
                        {
                            frameIndex = relativeIndex, batteryLevel = SystemInfo.batteryLevel,
                            memorySize = Profiler.maxUsedMemory, frame = _fps,
                            monoHeapSize = Profiler.GetMonoHeapSizeLong(),
                            monoUsedSize = Profiler.GetMonoUsedSizeLong(),
                            totalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong(),
                            totalUnusedReservedMemory = Profiler.GetTotalUnusedReservedMemoryLong(),
                            unityTotalReservedMemory = Profiler.GetTotalReservedMemoryLong(),
                            allocatedMemoryForGraphicsDriver = Profiler.GetAllocatedMemoryForGraphicsDriver()
                        };
                        uprofilerInfos.uProfilerInfoList.Add(uprofilerInfo);
#if UNITY_ANDROID && !UNITY_EDITOR
                    if (EnableMobileConsumptionInfo)
                    {
                        GetPowerConsume(relativeIndex);
                    }
                    if (EnableResMemoryDistributionInfo)
                    {
                        GetFramePssMemory(relativeIndex);
                    }
#endif
                        if (enableResMemoryDistributionInfo)
                            GetResMemoryInfo(relativeIndex);
                        if (enableFrameTexture) // TODO: async capture
                            ScreenCapture.CaptureScreenshot($"{captureFilePath}/img_{_startTime}_{relativeIndex}.png");
#if UNITY_2020_1_OR_NEWER
                        if (enableRenderInfo)
                            GetRenderInfo(relativeIndex);
#endif
                    }
                }
            }
        }

        void GetSystemInfo()
        {
            DeviceInfo deviceInfo = new DeviceInfo()
            {
                unityVersion = Application.unityVersion,
                deviceModel = SystemInfo.deviceModel,
                batteryLevel = SystemInfo.batteryLevel,
                deviceName = SystemInfo.deviceName,
                deviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor,
                graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                graphicsMemorySize = SystemInfo.graphicsMemorySize,
                operatingSystem = SystemInfo.operatingSystem,
                processorCount = SystemInfo.processorCount,
                processorFrequency = SystemInfo.processorFrequency,
                processorType = SystemInfo.processorType,
                supportsShadows = SystemInfo.supportsShadows,
                systemMemorySize = SystemInfo.systemMemorySize,
                screenHeight = Screen.height,
                screenWidth = Screen.width
            };
            bool writeRes = false;
            if (!useBinary)
            {
                writeRes = FileManager.WriteToFile(deviceFilePath, JsonUtility.ToJson(deviceInfo));
            }
            else
            {
                writeRes = FileManager.WriteBinaryDataToFile(deviceFilePath, deviceInfo);
            }

            if (writeRes)
            {
                UploadFile(deviceFilePath);
            }
        }

#if UNITY_2020_1_OR_NEWER
        void GetRenderInfo(int index)
        {
            var renderInfo = new RenderInfo()
            {
                frameIndex = index,
                drawCall = drawCallRecord.LastValue,
                setPassCall = setPassCallRecord.LastValue,
                triangles = trianglesRecord.LastValue,
                vertices = verticesRecord.LastValue
            };
            _renderInfos.renderInfoList.Add(renderInfo);
        }
#endif

        void GetResMemoryInfo(int index)
        {
            RecordResInfo record = new RecordResInfo();
            record.frameIndex = index;
            var pair = CollectResFrameDatas<Texture>.TakeSample();
            record.textureSize = pair.Key;
            record.totalSize += pair.Key;
            record.textureCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<Mesh>.TakeSample();
            record.meshSize = pair.Key;
            record.totalSize += pair.Key;
            record.meshCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<Material>.TakeSample();
            record.materialSize = pair.Key;
            record.totalSize += pair.Key;
            record.materialCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<Shader>.TakeSample();
            record.shaderSize = pair.Key;
            record.totalSize += pair.Key;
            record.shaderCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<AnimationClip>.TakeSample();
            record.animationClipSize = pair.Key;
            record.totalSize += pair.Key;
            record.animationClipCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<AudioClip>.TakeSample();
            record.audioClipSize = pair.Key;
            record.totalSize += pair.Key;
            record.audioClipCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<Texture>.TakeSample();
            record.textureSize = pair.Key;
            record.totalSize += pair.Key;
            record.textureCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<Font>.TakeSample();
            record.fontSize = pair.Key;
            record.totalSize += pair.Key;
            record.fontCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<TextAsset>.TakeSample();
            record.textAssetSize = pair.Key;
            record.totalSize += pair.Key;
            record.textAssetCount = pair.Value;
            record.totalCount += pair.Value;
            pair = CollectResFrameDatas<ScriptableObject>.TakeSample();
            record.scriptableObjectSize = pair.Key;
            record.totalSize += pair.Key;
            record.scriptableObjectCount = pair.Value;
            record.totalCount += pair.Value;
            _recordResInfos.recordResInfosList.Add(record);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
    /// <summary>Sample Android power consumption for the frame.</summary>
    void GetPowerConsume(int index)
    {
        //Debug.Log("GetPowerConsume");
        unityAndroidProxy ??= new UnityAndroidProxy();
        DevicePowerConsumeInfo devicePowerConsumeInfo = unityAndroidProxy.GetPowerConsumeInfo(index);
        //Debug.Log($"Power sample: {devicePowerConsumeInfo}");
        devicePowerConsumeInfos.devicePowerConsumeInfos.Add(devicePowerConsumeInfo);
    }

    void GetFramePssMemory(int index)
    {
        unityAndroidProxy ??= new UnityAndroidProxy();
        MemoryUseData data = unityAndroidProxy.GetPssMemory(index);
        memoryUseDatas.MemoryUsedList.Add(data);
    }
#endif

        /// <summary>Zip captured screenshots and upload.</summary>
        private void ZipCaptureFiles()
        {
            string srcFileDir = captureFilePath;
            string zipFilePath = captureFilePath + ".zip";
            bool zipSuccess = ZipUtils.ZipFile(srcFileDir, zipFilePath);
            Debug.Log($"Zipping capture folder: {zipFilePath}");

            if (zipSuccess)
            {
                UploadFile(zipFilePath);
                Debug.Log("Capture zip uploaded.");
            }
            else
            {
                Debug.LogError("Failed to zip capture folder!");
            }
        }

        void OnDisable()
        {
            if (enableRenderInfo)
            {
                setPassCallRecord.Dispose();
                drawCallRecord.Dispose();
                verticesRecord.Dispose();
                trianglesRecord.Dispose();
            }
        }
    }
}