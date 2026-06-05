using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LemonFramework.UProfiler.Core
{
    [Serializable]
    public class LogInfos : IBinarySerializable
    {
        public List<string> logList = new List<string>();
        private StringBuilder _stringBuilder = new StringBuilder();

        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                string str = reader.ReadString();
                logList.Add(str);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(logList.Count);
            for (int i = 0; i < logList.Count; i++)
            {
                writer.Write(logList[i]);
            }
        }

        public override string ToString()
        {
            _stringBuilder.Clear();
            for (int i = 0; i < logList.Count; i++)
            {
                _stringBuilder.AppendLine(logList[i]);
            }

            return _stringBuilder.ToString();
        }
    }
}