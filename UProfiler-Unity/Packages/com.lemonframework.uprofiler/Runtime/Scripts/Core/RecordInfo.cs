using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LemonFramework.UProfiler.Core
{
    [Serializable]
    public class RecordInfo : IBinarySerializable
    {
        public int frameIndex;
        public string name;
        public int count;
        public long size;

        public RecordInfo(string name)
        {
            this.name = name;
            count = 0;
            size = 0L;
        }

        public RecordInfo()
        {
        }

        public void DeSerialize(BinaryReader reader)
        {
            frameIndex = reader.ReadInt32();
            name = reader.ReadString();
            count = reader.ReadInt32();
            size = reader.ReadInt64();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(frameIndex);
            writer.Write(name);
            writer.Write(count);
            writer.Write(size);
        }

        public override string ToString()
        {
            return $"Name:{name} Count:{count} Size:{size}";
        }
    }

    //一帧里面的各个资源数据
    [Serializable]
    public class RecordInfos : IBinarySerializable
    {
        private long _size = 0L;
        private int _count = 0;
        private List<RecordInfo> _recordInfoList = new List<RecordInfo>();
        private StringBuilder _stringBuilder = new StringBuilder();

        public void AddInfo(RecordInfo info)
        {
            _recordInfoList.Add(info);
            _size += info.size;
            _count += info.count;
        }

        public void DeSerialize(BinaryReader reader)
        {
            _size = reader.ReadInt64();
            _count = reader.ReadInt32();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var tempData = new RecordInfo();
                tempData.DeSerialize(reader);
                _recordInfoList.Add(tempData);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(_size);
            writer.Write(_count);
            writer.Write(_recordInfoList.Count);
            for (int i = 0; i < _recordInfoList.Count; i++)
            {
                _recordInfoList[i].Serialize(writer);
            }
        }

        public override string ToString()
        {
            _stringBuilder.Clear();
            _stringBuilder.Append($"Count:{_count} Size:{_size}\n");
            for (int i = 0; i < _recordInfoList.Count; i++)
            {
                _stringBuilder.Append($"{_recordInfoList[i].ToString()}\n");
            }

            return _stringBuilder.ToString();
        }
    }

    /// <summary>
    /// 采样帧内存分类数据记录列表
    /// </summary>
    [Serializable]
    public class RecordInfosCollections : IBinarySerializable
    {
        public List<RecordInfos> recordInfosList = new List<RecordInfos>();
        private StringBuilder _stringBuilder = new StringBuilder();

        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                RecordInfos tempData = new RecordInfos();
                tempData.DeSerialize(reader);
                recordInfosList.Add(tempData);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(recordInfosList.Count);
            for (int i = 0; i < recordInfosList.Count; i++)
            {
                recordInfosList[i].Serialize(writer);
            }
        }

        public override string ToString()
        {
            _stringBuilder.Clear();
            for (int i = 0; i < recordInfosList.Count; i++)
            {
                _stringBuilder.Append($"{recordInfosList[i].ToString()}\n");
            }

            return _stringBuilder.ToString();
        }
    }

    [Serializable]
    public struct RecordResInfo : IBinarySerializable
    {
        public int frameIndex;
        public long textureSize;
        public int textureCount;
        public long meshSize;
        public int meshCount;
        public long materialSize;
        public int materialCount;
        public long shaderSize;
        public int shaderCount;
        public long animationClipSize;
        public int animationClipCount;
        public long audioClipSize;
        public int audioClipCount;
        public long fontSize;
        public int fontCount;
        public long textAssetSize;
        public int textAssetCount;
        public long scriptableObjectSize;
        public int scriptableObjectCount;
        public long totalSize; //统计部分的总量
        public int totalCount; //统计部分的数

        public void DeSerialize(BinaryReader reader)
        {
            frameIndex = reader.ReadInt32();
            textureSize = reader.ReadInt64();
            textureCount = reader.ReadInt32();
            meshSize = reader.ReadInt64();
            meshCount = reader.ReadInt32();
            materialSize = reader.ReadInt64();
            materialCount = reader.ReadInt32();
            shaderSize = reader.ReadInt64();
            shaderCount = reader.ReadInt32();
            animationClipSize = reader.ReadInt64();
            animationClipCount = reader.ReadInt32();
            audioClipSize = reader.ReadInt64();
            audioClipSize = reader.ReadInt32();
            fontSize = reader.ReadInt64();
            fontCount = reader.ReadInt32();
            textAssetSize = reader.ReadInt64();
            textAssetCount = reader.ReadInt32();
            scriptableObjectSize = reader.ReadInt64();
            scriptableObjectCount = reader.ReadInt32();
            totalSize = reader.ReadInt64();
            totalCount = reader.ReadInt32();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(frameIndex);
            writer.Write(textureSize);
            writer.Write(textureCount);
            writer.Write(meshSize);
            writer.Write(meshCount);
            writer.Write(materialSize);
            writer.Write(materialCount);
            writer.Write(shaderSize);
            writer.Write(shaderCount);
            writer.Write(animationClipSize);
            writer.Write(animationClipCount);
            writer.Write(audioClipSize);
            writer.Write(audioClipCount);
            writer.Write(fontSize);
            writer.Write(fontCount);
            writer.Write(textAssetSize);
            writer.Write(textAssetCount);
            writer.Write(scriptableObjectSize);
            writer.Write(scriptableObjectCount);
            writer.Write(totalSize);
            writer.Write(totalCount);
        }
    }

    [Serializable]
    public class RecordResInfos : IBinarySerializable
    {
        public List<RecordResInfo> recordResInfosList = new List<RecordResInfo>();

        public void DeSerialize(BinaryReader reader)
        {
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                var tempData = new RecordResInfo();
                tempData.DeSerialize(reader);
                recordResInfosList.Add(tempData);
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(recordResInfosList.Count);
            for (int i = 0; i < recordResInfosList.Count; i++)
            {
                recordResInfosList[i].Serialize(writer);
            }
        }
    }
}