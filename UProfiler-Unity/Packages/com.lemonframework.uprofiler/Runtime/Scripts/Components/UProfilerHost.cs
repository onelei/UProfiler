using LemonFramework.UProfiler.Core;
using System;
using System.Collections;
using System.IO;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace LemonFramework.UProfiler.Components
{
public class UProfilerHost : MonoBehaviour
{
    [Header("Enable log capture")]
    public bool EnableLog = false;
    [Header("Capture frame screenshots")]
    public bool EnableFrameTexture = false;
    [Header("Function analysis (requires IL inject via Hook menu)")]
    public bool EnableFunctionAnalysis = false;
    [Header("Mobile power / battery stats (Android)")]
    public bool EnableMobileConsumptionInfo = true;
    [Header("Resource memory distribution")]
    public bool EnableResMemoryDistributionInfo = true;
#if UNITY_2020_1_OR_NEWER
    [Header("Render stats (DrawCall, triangles)")]
    public bool EnableRenderInfo = true;
#endif
    [Header("Sample interval (frames)")]
    [Range(10, 1000)]
    public int IntervalFrame = 100;
    [Header("Frames to skip after start")]
    public int IgnoreFrameCount = 5;
    [Header("Use binary file format (.data instead of .txt)")]
    public bool UseBinary = false;

    public Text UploadTips;
    public Text ReportUrl;
    int m_FPS = 0;
    int m_TickTime = 0;
    string m_StartTime = "";
    float m_Accumulator = 0;
    int m_Frames = 0;
    float m_TimeLeft;
    float m_UpdateInterval = 0.5f;
    bool btnUProfiler = false;
    string btnMsg;
    int m_frameIndex = 0;
    Action<bool> UProfilerCallback;
    UProfilerInfos uprofilerInfos = null;
    FrameRates frameRateInfos = null;
#if UNITY_ANDROID && !UNITY_EDITOR
    MemoryUseDatas memoryUseDatas = null; // Android PSS samples
    //
    DevicePowerConsumeInfos devicePowerConsumeInfos = null;
#endif
#if UNITY_2020_1_OR_NEWER
    RenderInfos renderInfos = null;
#endif

    /// <summary>
    /// <summary>Per-frame resource memory samples.</summary>
    RecoreResInfos recordResInfos = null;
    //
    string funcAnalysisFilePath;
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
    //
    string fileExt;

#if UNITY_2020_1_OR_NEWER
    private ProfilerRecorder setPassCallRecord;
    private ProfilerRecorder drawCallRecord; // draw calls
    private ProfilerRecorder verticesRecord;//
    private ProfilerRecorder trianglesRecord;//

    //
    private ProfilerRecorder gcRecord;//gc

    //private ProfilerRecorder gcMemoryRecord;//gc
    //private ProfilerRecorder mainThreadTimeRecord;
#endif

    private UnityAndroidProxy unityAndroidProxy = null;
    void Awake()
    {
        Application.targetFrameRate = 60;
#if UNITY_2020_1_OR_NEWER
        if (EnableRenderInfo)
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
            fileExt = UseBinary ? ConstString.BinaryExt : ConstString.TextExt;
            Debug.Log(ConstString.UProfilerActive);
            m_frameIndex = 0;
            ShareDatas.StartTime = DateTime.Now; //
            m_StartTime = ShareDatas.StartTime.ToString("yyyy_MM_dd_HH_mm_ss");
            ShareDatas.StartTimeStr = m_StartTime;
#if UNITY_EDITOR
            PlayerPrefs.SetString("TestTime", m_StartTime);
            PlayerPrefs.Save();
#endif
            if (EnableFrameTexture)
            {
                captureFilePath = $"{Application.persistentDataPath}/{ConstString.CaptureFramePrefix}{m_StartTime}";
                FileManager.CreateDir(captureFilePath);
            }
            if (EnableFunctionAnalysis)
                funcAnalysisFilePath = $"{Application.persistentDataPath}/{ConstString.FuncAnalysisPrefix}{m_StartTime}{fileExt}";
            if (EnableLog)
                logFilePath = $"{Application.persistentDataPath}/{ConstString.LogPrefix}{m_StartTime}{fileExt}";
            deviceFilePath = $"{Application.persistentDataPath}/{ConstString.DevicePrefix}{m_StartTime}{fileExt}";
            testFilePath = $"{Application.persistentDataPath}/{ConstString.TestPrefix}{m_StartTime}{fileExt}";
            uprofilerFilePath = $"{Application.persistentDataPath}/{ConstString.UProfilerPrefix}{m_StartTime}{fileExt}";
            frameRateFilePath = $"{Application.persistentDataPath}/{ConstString.FrameRatefix}{m_StartTime}{fileExt}";
#if UNITY_ANDROID && !UNITY_EDITOR
            if (EnableMobileConsumptionInfo)
                powerConsumeFilePath = $"{Application.persistentDataPath}/{ConstString.PowerConsumePrefix}{m_StartTime}{fileExt}";
#endif
            if (EnableResMemoryDistributionInfo)
            {
                resMemoryDistributionPath = $"{Application.persistentDataPath}/{ConstString.ResMemoryDistributionPrefix}{m_StartTime}{fileExt}";
#if UNITY_ANDROID && !UNITY_EDITOR
                pssMemoryUsedFilePath = $"{Application.persistentDataPath}/{ConstString.PssMemoryPrefix}{m_StartTime}{fileExt}";
#endif
            }
#if UNITY_2020_1_OR_NEWER
            if (EnableRenderInfo)
                renderFilePath = $"{Application.persistentDataPath}/{ConstString.RenderPrefix}{m_StartTime}{fileExt}";
#endif
            if (EnableLog)
            {
                LogManager.CreateLogFile(logFilePath, System.IO.FileMode.Append);
                Application.logMessageReceived += LogManager.LogToFile;
            }

            m_TickTime = 0;
            InvokeRepeating("Tick", 1.0f, 1.0f);

            if (ReportUrl != null)
            {
                ReportUrl.gameObject.SetActive(false);
            }
            StartUProfiler();
        }
        else
        {
            Debug.Log(ConstString.UProfilerStop);
            ShareDatas.EndTime = DateTime.Now;
            //
            UploadTestInfo();
            //
            GetSystemInfo();

            CancelInvoke("Tick");
            m_TickTime = 0;

            UProfilerInfosReport();
            FrameRateInfosReport();
#if UNITY_2020_1_OR_NEWER
            if (EnableRenderInfo)
                RenderInfosReport();
#endif
            if (EnableResMemoryDistributionInfo)
                ResMemoryReport();
            if (EnableFunctionAnalysis)
                FuncAnalysisReport();
#if UNITY_ANDROID && !UNITY_EDITOR
            if (EnableMobileConsumptionInfo) //
            {
                MobileConsumptionInfoReport();
                MobilePssMemoryUseReport();
            }
#endif
            if (EnableFrameTexture)
                ZipCaptureFiles();

            if (EnableLog)
            {
                Application.logMessageReceived -= LogManager.LogToFile;
                LogManager.CloseLogFile();
            }

            if (EnableLog)
            {
                UploadFile(logFilePath);
            }

            HttpGet(string.Format(Config.ReportRecordUpdateRequestUrl, Application.identifier, m_StartTime), (result) =>
            {
                if (result)
                {
                    if (ReportUrl != null)
                    {
                        ReportUrl.gameObject.SetActive(true);
                        ReportUrl.text = $"<a href={string.Format(ShareDatas.ReportUrl, ShareDatas.StartTimeStr)}>{string.Format(ShareDatas.ReportUrl, ShareDatas.StartTimeStr)}</a>";
                    }
                }
            });
        }
    }

    [FunctionAnalysis]
    void Start()
    {
        btnMsg = ConstString.UProfilerBegin;
        GameObject.DontDestroyOnLoad(gameObject);
        UProfilerCallback += UProfilerCallBackFunc;
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
            ProductName = $"{Application.companyName}.{Application.productName}",
            PackageName = Application.identifier,
            Platform = Application.platform.ToString(),
            Version = Application.version,
            TestTime = ShareDatas.GetTestTime(),
            IntervalFrame = this.IntervalFrame
        };
        // Legacy HTML test info export removed; JSON/binary via FileManager.WriteToFile above.
        bool writeRes = false;
        if (!UseBinary)
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
        renderInfos = new RenderInfos();
#if UNITY_ANDROID && !UNITY_EDITOR
        devicePowerConsumeInfos = new DevicePowerConsumeInfos();
        memoryUseDatas = new MemoryUseDatas();
#endif
        recordResInfos = new RecoreResInfos();
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

    void FuncAnalysisReport()
    {
        HookUtil.MethodAnalysisReport(m_StartTime);
        if (File.Exists(funcAnalysisFilePath))
        {
            UploadFile(funcAnalysisFilePath);
        }
        else
        {
            Debug.LogError($"Function analysis file missing: {funcAnalysisFilePath}");
        }
    }

    void ResMemoryReport()
    {
        bool writeRes = false;
        if (!UseBinary)
        {
            writeRes = FileManager.WriteToFile(resMemoryDistributionPath, JsonUtility.ToJson(recordResInfos));
        }
        else
        {
            writeRes = FileManager.WriteBinaryDataToFile(resMemoryDistributionPath, recordResInfos);
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
        if (!UseBinary)
        {
            writeRes = FileManager.WriteToFile(renderFilePath, JsonUtility.ToJson(renderInfos));
        }
        else
        {
            writeRes = FileManager.WriteBinaryDataToFile(renderFilePath, renderInfos);
        }
        if (writeRes)
        {
            UploadFile(renderFilePath);
        }
    }
#endif

    void FrameRateInfosReport()
    {
        bool writeRes = false;
        if (!UseBinary)
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
        if (!UseBinary)
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
        m_TickTime++;
    }

    void UploadFile(string filePath)
    {
        if (Config.UseFtpUpload)
        {
            FileFTPUploadManager.UploadFile(filePath, (sender, e) =>
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
            }, (sender, e) =>
            {
                Debug.Log($"File Uploaded :{e.Result}");
            });
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
            btnMsg = btnUProfiler ? ConstString.UProfilerActive : ConstString.UProfilerBegin;
            if (UProfilerCallback != null)
                UProfilerCallback.Invoke(btnUProfiler);
        }
        if (btnUProfiler)
            btnMsg = $"{ConstString.UProfilerActive}{m_TickTime}s";
        GUI.Label(new Rect(Screen.width / 2, 0, 100, 100), "FPS:" + m_FPS);
    }

    [HideAnalysis]
    void Update()
    {
        m_Frames++;
        m_Accumulator += Time.unscaledDeltaTime;
        m_TimeLeft -= Time.unscaledDeltaTime;
        if (m_TimeLeft <= 0f)
        {
            m_FPS = (int)(m_Accumulator > 0f ? m_Frames / m_Accumulator : 0f);
            m_Frames = 0;
            m_Accumulator = 0f;
            m_TimeLeft += m_UpdateInterval;
        }

        if (btnUProfiler)
        {
            ++m_frameIndex;
            if (m_frameIndex > IgnoreFrameCount)
            {
                var relativeIndex = m_frameIndex - IgnoreFrameCount;
                frameRateInfos.FrameRateList.Add(new UProfilerFrameInfo() { FrameIndex = relativeIndex, Frame = m_FPS });
                if ((m_frameIndex - IgnoreFrameCount) % IntervalFrame == 0)
                {
                    var uprofilerInfo = new UProfilerInfo() { FrameIndex = relativeIndex, BatteryLevel = SystemInfo.batteryLevel, MemorySize = Profiler.maxUsedMemory, Frame = m_FPS, MonoHeapSize = Profiler.GetMonoHeapSizeLong(), MonoUsedSize = Profiler.GetMonoUsedSizeLong(), TotalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong(), TotalUnusedReservedMemory = Profiler.GetTotalUnusedReservedMemoryLong(), UnityTotalReservedMemory = Profiler.GetTotalReservedMemoryLong(), AllocatedMemoryForGraphicsDriver = Profiler.GetAllocatedMemoryForGraphicsDriver() };
                    uprofilerInfos.UProfilerInfoList.Add(uprofilerInfo);
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
                    if (EnableResMemoryDistributionInfo)
                        GetResMemoryInfo(relativeIndex);
                    if (EnableFrameTexture) // TODO: async capture
                        ScreenCapture.CaptureScreenshot($"{captureFilePath}/img_{m_StartTime}_{relativeIndex}.png");
#if UNITY_2020_1_OR_NEWER
                    if (EnableRenderInfo)
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
            UnityVersion = Application.unityVersion,
            DeviceModel = SystemInfo.deviceModel,
            BatteryLevel = SystemInfo.batteryLevel,
            DeviceName = SystemInfo.deviceName,
            DeviceUniqueIdentifier = SystemInfo.deviceUniqueIdentifier,
            GraphicsDeviceName = SystemInfo.graphicsDeviceName,
            GraphicsDeviceVendor = SystemInfo.graphicsDeviceVendor,
            GraphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
            GraphicsMemorySize = SystemInfo.graphicsMemorySize,
            OperatingSystem = SystemInfo.operatingSystem,
            ProcessorCount = SystemInfo.processorCount,
            ProcessorFrequency = SystemInfo.processorFrequency,
            ProcessorType = SystemInfo.processorType,
            SupportsShadows = SystemInfo.supportsShadows,
            SystemMemorySize = SystemInfo.systemMemorySize,
            ScreenHeight = Screen.height,
            ScreenWidth = Screen.width
        };
        bool writeRes = false;
        if (!UseBinary)
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
        var renderInfo = new RenderInfo() { FrameIndex = index, DrawCall = drawCallRecord.LastValue, SetPassCall = setPassCallRecord.LastValue, Triangles = trianglesRecord.LastValue, Vertices = verticesRecord.LastValue };
        renderInfos.RenderInfoList.Add(renderInfo);
    }
#endif

    void GetResMemoryInfo(int index)
    {
        RecordResInfo record = new RecordResInfo();
        record.FrameIndex = index;
        var pair = CollectResFrameDatas<Texture>.TakeSample();
        record.TextureSize = pair.Key;
        record.TotalSize += pair.Key;
        record.TextureCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<Mesh>.TakeSample();
        record.MeshSize = pair.Key;
        record.TotalSize += pair.Key;
        record.MeshCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<Material>.TakeSample();
        record.MaterialSize = pair.Key;
        record.TotalSize += pair.Key;
        record.MaterialCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<Shader>.TakeSample();
        record.ShaderSize = pair.Key;
        record.TotalSize += pair.Key;
        record.ShaderCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<AnimationClip>.TakeSample();
        record.AnimationClipSize = pair.Key;
        record.TotalSize += pair.Key;
        record.AnimationClipCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<AudioClip>.TakeSample();
        record.AudioClipSize = pair.Key;
        record.TotalSize += pair.Key;
        record.AudioClipCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<Texture>.TakeSample();
        record.TextureSize = pair.Key;
        record.TotalSize += pair.Key;
        record.TextureCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<Font>.TakeSample();
        record.FontSize = pair.Key;
        record.TotalSize += pair.Key;
        record.FontCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<TextAsset>.TakeSample();
        record.TextAssetSize = pair.Key;
        record.TotalSize += pair.Key;
        record.TextAssetCount = pair.Value;
        record.TotalCount += pair.Value;
        pair = CollectResFrameDatas<ScriptableObject>.TakeSample();
        record.ScriptableObjectSize = pair.Key;
        record.TotalSize += pair.Key;
        record.ScriptableObjectCount = pair.Value;
        record.TotalCount += pair.Value;
        recordResInfos.RecordResInfosList.Add(record);
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

    private void OnDestroy()
    {
        UProfilerCallback -= UProfilerCallBackFunc;
    }

    void OnDisable()
    {
        if (EnableRenderInfo)
        {
            setPassCallRecord.Dispose();
            drawCallRecord.Dispose();
            verticesRecord.Dispose();
            trianglesRecord.Dispose();
        }
    }
}
}
