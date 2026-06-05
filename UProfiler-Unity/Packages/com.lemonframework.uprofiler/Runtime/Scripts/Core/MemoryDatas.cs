using System;
using System.Collections.Generic;
using System.IO;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>
    /// 内存使用信息
    /// </summary>
    [Serializable]
    public struct MemoryUseData : IBinarySerializable
    {
        public int frameIndex;
        public float pssMemorySize; //M

        public void DeSerialize(BinaryReader reader)
        {
            frameIndex = reader.ReadInt32();
            pssMemorySize = reader.ReadSingle();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(frameIndex);
            writer.Write(pssMemorySize);
        }
    }

    /// <summary>
    /// PSS内存使用
    /// </summary>
    [Serializable]
    public class MemoryUseDatas : IBinarySerializable
    {
        public List<MemoryUseData> memoryUsedList = new List<MemoryUseData>();

        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var tempData = new MemoryUseData();
                tempData.DeSerialize(reader);
                memoryUsedList.Add(tempData);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(memoryUsedList.Count);
            for (int i = 0; i < memoryUsedList.Count; i++)
            {
                memoryUsedList[i].Serialize(writer);
            }
        }
    }
}