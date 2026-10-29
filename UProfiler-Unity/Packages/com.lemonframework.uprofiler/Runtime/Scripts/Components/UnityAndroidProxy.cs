#if UPROFILER
using System;
using LemonFramework.UProfiler.Core;
using UnityEngine;

namespace LemonFramework.UProfiler.Components
{
    public class UnityAndroidProxy
    {
        private AndroidJavaClass jc;
        private AndroidJavaObject jo;
#if UNITY_ANDROID
        private bool isInit;
#endif

        public UnityAndroidProxy()
        {
            Init();
        }

        public void Init()
        {
#if UNITY_ANDROID
            if (isInit)
            {
                return;
            }

            try
            {
                jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                //jc = new AndroidJavaClass("com.sc.testextendlibrary.MainActivity");
                jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
                isInit = true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
#endif
        }

#if UNITY_ANDROID
    public DevicePowerConsumeInfo GetPowerConsumeInfo(int frameIndex = 0)
    {
        if (null == jo || null == jc)
        {
            Init();
        }
        string result = jo.Call<string>("GetCurPowerConsumeArgs");
        if (string.IsNullOrEmpty(result))
        {
            return default;
        }
        //Debug.Log($"从安卓获取结�?{result}");
        string[] args = result.Split('|');
        DevicePowerConsumeInfo devicePowerConsumeInfo = new DevicePowerConsumeInfo();
        devicePowerConsumeInfo.frameIndex = frameIndex;
        devicePowerConsumeInfo.capacity = Convert.ToInt32(args[0]);
        devicePowerConsumeInfo.temperature = Convert.ToInt32(args[1]);
        devicePowerConsumeInfo.batteryV = Convert.ToSingle(args[2]);
        devicePowerConsumeInfo.batteryCapacity = Convert.ToInt32(args[3]);
        devicePowerConsumeInfo.batteryChargeCounter = Convert.ToInt32(args[4]);
        devicePowerConsumeInfo.batteryCurrentNow = Convert.ToInt32(args[5]);
        devicePowerConsumeInfo.batteryPower = Convert.ToSingle(args[6]);
        devicePowerConsumeInfo.useLeftHours = Convert.ToSingle(args[7]);
        devicePowerConsumeInfo.cpuTemperate = Convert.ToInt32(args[8]);
        return devicePowerConsumeInfo;
    }

    public MemoryUseData GetPssMemory(int frameIndex = 0)
    {
        if (null == jo || null == jc)
        {
            Init();
        }
        var pss = jo.Call<float>("GetCurAppMemorySize");
        var memoryUseData = new MemoryUseData()
        {
            frameIndex = frameIndex,
            pssMemorySize = pss
        };
        return memoryUseData;
    }
#endif
    }
}
#endif
