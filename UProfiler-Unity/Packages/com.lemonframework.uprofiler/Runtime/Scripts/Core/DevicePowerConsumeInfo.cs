using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LemonFramework.UProfiler.Core
{
    [Serializable]
    public struct DevicePowerConsumeInfo : IBinarySerializable
    {
        /// <summary>Frame index.</summary>
        public int FrameIndex;
        /// <summary>Battery capacity.</summary>
        public int Capacity;
        /// <summary>Temperature.</summary>
        public int Temperature;
        /// <summary>Battery voltage.</summary>
        public float BatteryV;
        /// <summary>Battery capacity (mAh).</summary>
        public int BatteryCapacity;
        /// <summary>Battery charge counter.</summary>
        public int BatteryChargeCounter;
        /// <summary>Battery current (now).</summary>
        public int BatteryCurrentNow;
        /// <summary>Battery power (W).</summary>
        public float BatteryPower;
        /// <summary>Estimated hours remaining.</summary>
        public float UseLeftHours;
        /// <summary>CPU temperature.</summary>
        public int CpuTemperate;

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(FrameIndex);
            writer.Write(Capacity);
            writer.Write(Temperature);
            writer.Write(BatteryV);
            writer.Write(BatteryCapacity);
            writer.Write(BatteryChargeCounter);
            writer.Write(BatteryCurrentNow);
            writer.Write(BatteryPower);
            writer.Write(UseLeftHours);
            writer.Write(CpuTemperate);
        }

        public void DeSerialize(BinaryReader reader)
        {
            FrameIndex = reader.ReadInt32();
            Capacity = reader.ReadInt32();
            Temperature = reader.ReadInt32();
            BatteryV = reader.ReadSingle();
            BatteryCapacity = reader.ReadInt32();
            BatteryChargeCounter = reader.ReadInt32();
            BatteryCurrentNow = reader.ReadInt32();
            BatteryPower = reader.ReadSingle();
            UseLeftHours = reader.ReadSingle();
            CpuTemperate = reader.ReadInt32();
        }

        public override string ToString()
        {
            return string.Format(
                "FrameIndex:{0}\n" +
                "Capacity:{1}\n" +
                "Temperature:{2}\n" +
                "BatteryV:{3}\n" +
                "BatteryCapacity:{4}\n" +
                "BatteryChargeCounter:{5}\n" +
                "BatteryCurrentNow:{6}\n" +
                "BatteryPower:{7}\n" +
                "UseLeftHours:{8}\n" +
                "CpuTemperate:{9}\n",
                FrameIndex,
                Capacity,
                Temperature,
                BatteryV,
                BatteryCapacity,
                BatteryChargeCounter,
                BatteryCurrentNow,
                BatteryPower,
                UseLeftHours,
                CpuTemperate);
        }
    }

    [Serializable]
    public class DevicePowerConsumeInfos : IBinarySerializable
    {
        public List<DevicePowerConsumeInfo> devicePowerConsumeInfos = new List<DevicePowerConsumeInfo>();

        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                DevicePowerConsumeInfo tempData = new DevicePowerConsumeInfo();
                tempData.DeSerialize(reader);
                devicePowerConsumeInfos.Add(tempData);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(devicePowerConsumeInfos.Count);
            for (int i = 0; i < devicePowerConsumeInfos.Count; i++)
            {
                devicePowerConsumeInfos[i].Serialize(writer);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < devicePowerConsumeInfos.Count; i++)
            {
                sb.Append(devicePowerConsumeInfos[i].ToString());
                sb.Append('\n');
            }
            return sb.ToString();
        }
    }
}
