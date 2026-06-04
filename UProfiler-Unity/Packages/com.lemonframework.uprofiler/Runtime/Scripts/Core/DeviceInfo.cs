using System;
using System.IO;

namespace LemonFramework.UProfiler.Core
{
    [Serializable]
    public struct DeviceInfo : IBinarySerializable
    {
        /// <summary>Unity version.</summary>
        public string UnityVersion;
        /// <summary>Operating system.</summary>
        public string OperatingSystem;
        /// <summary>Device model.</summary>
        public string DeviceModel;
        /// <summary>Device name.</summary>
        public string DeviceName;
        /// <summary>Unique device identifier.</summary>
        public string DeviceUniqueIdentifier;
        /// <summary>System memory size (MB).</summary>
        public int SystemMemorySize;
        /// <summary>Graphics memory size (MB).</summary>
        public int GraphicsMemorySize;
        /// <summary>Processor type.</summary>
        public string ProcessorType;
        /// <summary>Processor frequency (MHz).</summary>
        public int ProcessorFrequency;
        /// <summary>Processor count.</summary>
        public int ProcessorCount;
        /// <summary>Graphics device name.</summary>
        public string GraphicsDeviceName;
        /// <summary>Graphics device vendor.</summary>
        public string GraphicsDeviceVendor;
        /// <summary>Graphics API / driver version.</summary>
        public string GraphicsDeviceVersion;
        /// <summary>Whether shadows are supported.</summary>
        public bool SupportsShadows;
        /// <summary>Battery level (0-1).</summary>
        public float BatteryLevel;
        /// <summary>Screen width (pixels).</summary>
        public int ScreenWidth;
        /// <summary>Screen height (pixels).</summary>
        public int ScreenHeight;

        public void DeSerialize(BinaryReader reader)
        {
            UnityVersion = reader.ReadString();
            OperatingSystem = reader.ReadString();
            DeviceModel = reader.ReadString();
            DeviceName = reader.ReadString();
            DeviceUniqueIdentifier = reader.ReadString();
            SystemMemorySize = reader.ReadInt32();
            GraphicsMemorySize = reader.ReadInt32();
            ProcessorType = reader.ReadString();
            ProcessorFrequency = reader.ReadInt32();
            ProcessorCount = reader.ReadInt32();
            GraphicsDeviceName = reader.ReadString();
            GraphicsDeviceVendor = reader.ReadString();
            GraphicsDeviceVersion = reader.ReadString();
            SupportsShadows = reader.ReadBoolean();
            BatteryLevel = reader.ReadSingle();
            ScreenWidth = reader.ReadInt32();
            ScreenHeight = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(UnityVersion);
            writer.Write(OperatingSystem);
            writer.Write(DeviceModel);
            writer.Write(DeviceName);
            writer.Write(DeviceUniqueIdentifier);
            writer.Write(SystemMemorySize);
            writer.Write(GraphicsMemorySize);
            writer.Write(ProcessorType);
            writer.Write(ProcessorFrequency);
            writer.Write(ProcessorCount);
            writer.Write(GraphicsDeviceName);
            writer.Write(GraphicsDeviceVendor);
            writer.Write(GraphicsDeviceVersion);
            writer.Write(SupportsShadows);
            writer.Write(BatteryLevel);
            writer.Write(ScreenWidth);
            writer.Write(ScreenHeight);
        }

        public override string ToString()
        {
            return string.Format(
                "UnityVersion:{0}\n" +
                "OperatingSystem:{1}\n" +
                "DeviceModel:{2}\n" +
                "DeviceName:{3}\n" +
                "DeviceUniqueIdentifier:{4}\n" +
                "SystemMemorySize:{5}\n" +
                "GraphicsMemorySize:{6}\n" +
                "ProcessorType:{7}\n" +
                "ProcessorFrequency:{8}\n" +
                "ProcessorCount:{9}\n" +
                "GraphicsDeviceName:{10}\n" +
                "GraphicsDeviceVendor:{11}\n" +
                "GraphicsDeviceVersion:{12}\n" +
                "SupportsShadows:{13}\n" +
                "BatteryLevel:{14}\n" +
                "ScreenWidth:{15}\n" +
                "ScreenHeight:{16}",
                UnityVersion,
                OperatingSystem,
                DeviceModel,
                DeviceName,
                DeviceUniqueIdentifier,
                SystemMemorySize,
                GraphicsMemorySize,
                ProcessorType,
                ProcessorFrequency,
                ProcessorCount,
                GraphicsDeviceName,
                GraphicsDeviceVendor,
                GraphicsDeviceVersion,
                SupportsShadows,
                BatteryLevel,
                ScreenWidth,
                ScreenHeight);
        }
    }
}
