using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LemonFramework.UProfiler.Core
{
    // 帧率信息
    [Serializable]
    public struct UProfilerFrameInfo : IBinarySerializable
    {
        public int frameIndex;
        public int frame;

        public void DeSerialize(BinaryReader reader)
        {
            frameIndex = reader.ReadInt32();
            frame = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(frameIndex);
            writer.Write(frame);
        }
    }

    [Serializable]
    public class FrameRates : IBinarySerializable
    {
        public List<UProfilerFrameInfo> frameRateList = new List<UProfilerFrameInfo>();
        private StringBuilder _stringBuilder = new StringBuilder();

        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                UProfilerFrameInfo tempData = new UProfilerFrameInfo();
                tempData.DeSerialize(reader);
                frameRateList.Add(tempData);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(frameRateList.Count);
            for (int i = 0; i < frameRateList.Count; i++)
            {
                frameRateList[i].Serialize(writer);
            }
        }

        public override string ToString()
        {
            _stringBuilder.Clear();
            for (int i = 0; i < frameRateList.Count; i++)
            {
                _stringBuilder.Append($"{frameRateList[i].ToString()}\n");
            }

            return _stringBuilder.ToString();
        }
    }

    [Serializable]
    public struct UProfilerInfo : IBinarySerializable
    {
        public int frameIndex;
        public float batteryLevel;
        public int memorySize;

        public int frame;

        // 托管堆内存
        public long monoHeapSize;

        // Mono堆内存使用
        public long monoUsedSize;

        public long allocatedMemoryForGraphicsDriver;

        // Unity分配的内存
        public long totalAllocatedMemory;

        // Unity保留的总内存
        public long unityTotalReservedMemory;

        // 未使用内存
        public long totalUnusedReservedMemory;

        public void DeSerialize(BinaryReader reader)
        {
            frameIndex = reader.ReadInt32();
            batteryLevel = reader.ReadSingle();
            memorySize = reader.ReadInt32();
            frame = reader.ReadInt32();
            monoHeapSize = reader.ReadInt64();
            monoUsedSize = reader.ReadInt64();
            allocatedMemoryForGraphicsDriver = reader.ReadInt64();
            totalAllocatedMemory = reader.ReadInt64();
            unityTotalReservedMemory = reader.ReadInt64();
            totalUnusedReservedMemory = reader.ReadInt64();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(frameIndex);
            writer.Write(batteryLevel);
            writer.Write(memorySize);
            writer.Write(frame);
            writer.Write(monoHeapSize);
            writer.Write(monoUsedSize);
            writer.Write(allocatedMemoryForGraphicsDriver);
            writer.Write(totalAllocatedMemory);
            writer.Write(unityTotalReservedMemory);
            writer.Write(totalUnusedReservedMemory);
        }

        public override string ToString()
        {
            return
                $"Frame:{frameIndex} BatteryLevel:{batteryLevel} MemorySize:{memorySize} Frame:{frame} MonoHeapSize:{monoHeapSize} MonoUsedSize:{monoUsedSize} AllocatedMemoryForGraphicsDriver:{allocatedMemoryForGraphicsDriver} TotalAllocatedMemory:{totalAllocatedMemory} UnityTotalReservedMemory:{unityTotalReservedMemory} TotalUnusedReservedMemory:{totalUnusedReservedMemory}";
        }
    }

    [Serializable]
    public class UProfilerInfos : IBinarySerializable
    {
        public List<UProfilerInfo> uProfilerInfoList = new List<UProfilerInfo>();
        private StringBuilder _stringBuilder = new StringBuilder();

        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                UProfilerInfo tempData = new UProfilerInfo();
                tempData.DeSerialize(reader);
                uProfilerInfoList.Add(tempData);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(uProfilerInfoList.Count);
            for (int i = 0; i < uProfilerInfoList.Count; i++)
            {
                uProfilerInfoList[i].Serialize(writer);
            }
        }

        public override string ToString()
        {
            _stringBuilder.Clear();
            for (int i = 0; i < uProfilerInfoList.Count; i++)
            {
                _stringBuilder.Append($"{uProfilerInfoList[i].ToString()}\n");
            }

            return _stringBuilder.ToString();
        }
    }
}