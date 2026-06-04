using System;
using System.IO;

namespace LemonFramework.UProfiler.Core
{
    /// <summary>Test session metadata.</summary>
    [Serializable]
    public struct TestInfo : IBinarySerializable
    {
        public string ProductName;
        public string PackageName;
        public string Platform;
        public string Version;
        public string TestTime;
        /// <summary>Sample interval (frames).</summary>
        public int IntervalFrame;

        public override string ToString()
        {
            return string.Format(
                "ProductName:{0}\n" +
                "PackageName:{1}\n" +
                "Platform:{2}\n" +
                "Version:{3}\n" +
                "TestTime:{4}\n" +
                "IntervalFrame:{5}",
                ProductName,
                PackageName,
                Platform,
                Version,
                TestTime,
                IntervalFrame);
        }

        public void DeSerialize(BinaryReader reader)
        {
            ProductName = reader.ReadString();
            PackageName = reader.ReadString();
            Platform = reader.ReadString();
            Version = reader.ReadString();
            TestTime = reader.ReadString();
            IntervalFrame = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(ProductName);
            writer.Write(PackageName);
            writer.Write(Platform);
            writer.Write(Version);
            writer.Write(TestTime);
            writer.Write(IntervalFrame);
        }
    }
}
