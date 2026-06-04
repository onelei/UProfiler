using System;
using System.IO;

namespace LemonFramework.UProfiler.Core
{
    [Serializable]
    public struct FuncAnalysisInfo : IBinarySerializable
    {
        public string Name;
        public double Memory;
        public double AverageMemory;
        public float UseTime;
        /// <summary>Average time per call (ms).</summary>
        public float AverageTime;
        /// <summary>Call count.</summary>
        public int Calls;

        public void DeSerialize(BinaryReader reader)
        {
            Name = reader.ReadString();
            Memory = reader.ReadDouble();
            AverageMemory = reader.ReadDouble();
            UseTime = reader.ReadSingle();
            AverageTime = reader.ReadSingle();
            Calls = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Name);
            writer.Write(Memory);
            writer.Write(AverageMemory);
            writer.Write(UseTime);
            writer.Write(AverageTime);
            writer.Write(Calls);
        }

        public override string ToString()
        {
            return string.Format(
                "Name:{0} Memory:{1}kb AvgMemory:{2}kb UseTime:{3}s AvgTime:{4}ms Calls:{5}",
                Name, Memory, AverageMemory, UseTime, AverageTime, Calls);
        }
    }
}
