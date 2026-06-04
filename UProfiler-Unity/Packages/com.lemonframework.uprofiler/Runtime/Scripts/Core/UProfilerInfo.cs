using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>
    /// 帧率信息
    /// </summary>
    [Serializable]
    public struct UProfilerFrameInfo : IBinarySerializable
    {
        public int FrameIndex;
        public int Frame;

        public void DeSerialize(BinaryReader reader)
        {
            FrameIndex = reader.ReadInt32();
            Frame = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FrameIndex);
            writer.Write(Frame);
        }
    }

    public class FrameRates : IBinarySerializable
    {
        public List<UProfilerFrameInfo> FrameRateList = new List<UProfilerFrameInfo>();
        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                UProfilerFrameInfo tempData = new UProfilerFrameInfo();
                tempData.DeSerialize(reader);
                FrameRateList.Add(tempData);
            }
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FrameRateList.Count);
            for (int i = 0; i < FrameRateList.Count; i++)
            {
                FrameRateList[i].Serialize(writer);
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < FrameRateList.Count; i++)
            {
                sb.Append($"{FrameRateList[i].ToString()}\n");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// 内存信息
    /// </summary>
    [Serializable]
    public struct UProfilerMemoryInfo
    {
        public int FrameIndex;
        public int MemorySize;
    }

    /// <summary>
    /// 电量信息
    /// </summary>
    [Serializable]
    public struct UProfilerBatteryLevelInfo
    {
        public int FrameIndex;
        public float BatteryLevel;
    }

    [Serializable]
    public struct UProfilerInfo : IBinarySerializable
    {
        public int FrameIndex;
        public float BatteryLevel;
        public int MemorySize;
        public int Frame;
        /// <summary>
        /// 托管堆内�?
        /// </summary>
        public long MonoHeapSize;
        /// <summary>
        /// Mono堆内存使用大�?
        /// </summary>
        public long MonoUsedSize;

        public long AllocatedMemoryForGraphicsDriver;
        /// <summary>
        /// Unity分配的内�?
        /// </summary>
        public long TotalAllocatedMemory;
        /// <summary>
        /// Unity保留的总内�?
        /// </summary>
        public long UnityTotalReservedMemory;
        /// <summary>
        /// 未使用内�?
        /// </summary>
        public long TotalUnusedReservedMemory;


        public void DeSerialize(BinaryReader reader)
        {
            FrameIndex = reader.ReadInt32();
            BatteryLevel = reader.ReadSingle();
            MemorySize = reader.ReadInt32();
            Frame = reader.ReadInt32();
            MonoHeapSize = reader.ReadInt64();
            MonoUsedSize = reader.ReadInt64();
            AllocatedMemoryForGraphicsDriver = reader.ReadInt64();
            TotalAllocatedMemory = reader.ReadInt64();
            UnityTotalReservedMemory = reader.ReadInt64();
            TotalUnusedReservedMemory = reader.ReadInt64();
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FrameIndex);
            writer.Write(BatteryLevel);
            writer.Write(MemorySize);
            writer.Write(Frame);
            writer.Write(MonoHeapSize);
            writer.Write(MonoUsedSize);
            writer.Write(AllocatedMemoryForGraphicsDriver);
            writer.Write(TotalAllocatedMemory);
            writer.Write(UnityTotalReservedMemory);
            writer.Write(TotalUnusedReservedMemory);
        }

        public override string ToString()
        {
            return $"Frame:{FrameIndex} BatteryLevel:{BatteryLevel} MemorySize:{MemorySize} Frame:{Frame} MonoHeapSize:{MonoHeapSize} MonoUsedSize:{MonoUsedSize} AllocatedMemoryForGraphicsDriver:{AllocatedMemoryForGraphicsDriver} TotalAllocatedMemory:{TotalAllocatedMemory} UnityTotalReservedMemory:{UnityTotalReservedMemory} TotalUnusedReservedMemory:{TotalUnusedReservedMemory}";
        }
    }

    [Serializable]
    public class UProfilerInfos : IBinarySerializable
    {
        public List<UProfilerInfo> UProfilerInfoList = new List<UProfilerInfo>();
        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                UProfilerInfo tempData = new UProfilerInfo();
                tempData.DeSerialize(reader);
                UProfilerInfoList.Add(tempData);
            }
        }
        public void Serialize(BinaryWriter writer)
        {
            writer.Write(UProfilerInfoList.Count);
            for (int i = 0; i < UProfilerInfoList.Count; i++)
            {
                UProfilerInfoList[i].Serialize(writer);
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < UProfilerInfoList.Count; i++)
            {
                sb.Append($"{UProfilerInfoList[i].ToString()}\n");
            }
            return sb.ToString();
        }
    }
}
