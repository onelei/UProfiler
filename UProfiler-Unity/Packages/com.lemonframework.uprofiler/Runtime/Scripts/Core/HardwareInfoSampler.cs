using System;
using UnityEngine;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>Samples CPU frequency, network type and low-memory events for hardwareInfo_ upload.</summary>
    public sealed class HardwareInfoSampler
    {
        bool _lowMemoryFlag;

        public HardwareInfoUploadData Data { get; } = new HardwareInfoUploadData();

        public HardwareInfoSampler()
        {
            Data.targetFrameRate = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60;
            Data.networkType = ResolveNetworkType();
            Application.lowMemory += OnLowMemory;
        }

        void OnLowMemory()
        {
            _lowMemoryFlag = true;
        }

        public void Sample(int frameIndex)
        {
            Data.samples.Add(new HardwareSampleRow
            {
                frameIndex = frameIndex,
                cpuFreqMHz = SystemInfo.processorFrequency > 0 ? SystemInfo.processorFrequency : 0,
                netSentKB = 0,
                netRecvKB = 0,
                lowMemory = _lowMemoryFlag
            });
            _lowMemoryFlag = false;
        }

        public void Dispose()
        {
            Application.lowMemory -= OnLowMemory;
        }

        static string ResolveNetworkType()
        {
            return Application.internetReachability switch
            {
                NetworkReachability.ReachableViaLocalAreaNetwork => "WIFI",
                NetworkReachability.ReachableViaCarrierDataNetwork => "Mobile",
                _ => "None"
            };
        }
    }
}
