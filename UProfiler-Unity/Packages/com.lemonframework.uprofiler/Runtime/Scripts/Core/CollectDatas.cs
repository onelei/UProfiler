using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LemonFramework.UProfiler.Core
{
    public static class CollectResFrameDatas<T> where T : UnityEngine.Object
    {
        public static KeyValuePair<long, int> TakeSample()
        {
            long size = 0L;
            int count = 0;
            T[] samples = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < samples.Length; i++)
            {
                long sampleSize = Profiler.GetRuntimeMemorySizeLong(samples[i]);
                string name = samples[i].GetType().Name;
                count++;
                size += sampleSize;
            }

            return new KeyValuePair<long, int>(size, count);
        }
    }

    public class CollectDatas<T> where T : UnityEngine.Object
    {
        private readonly List<RecordInfo> _records = new List<RecordInfo>();
        private readonly Comparison<RecordInfo> _recordComparer = RecordComparer;
        private DateTime _sampleTime = DateTime.MinValue;
        private int _sampleCount = 0;
        private long _sampleSize = 0L;

        public void TakeSample()
        {
            _records.Clear();
            _sampleTime = DateTime.UtcNow;
            _sampleCount = 0;
            _sampleSize = 0L;
            //获取所有类型的资源
            T[] samples = Resources.FindObjectsOfTypeAll<T>();
            for (int i = 0; i < samples.Length; i++)
            {
                long sampleSize = Profiler.GetRuntimeMemorySizeLong(samples[i]);
                string name = samples[i].GetType().Name;
                _sampleCount++;
                _sampleSize += sampleSize;

                RecordInfo record = null;
                foreach (RecordInfo r in _records)
                {
                    if (r.name == name)
                    {
                        record = r;
                        break;
                    }
                }

                if (record == null)
                {
                    record = new RecordInfo(name);
                    _records.Add(record);
                }

                record.count++;
                record.size += sampleSize;
            }

            _records.Sort(_recordComparer);
        }

        private static int RecordComparer(RecordInfo a, RecordInfo b)
        {
            int result = b.size.CompareTo(a.size);
            if (result != 0)
            {
                return result;
            }

            result = a.count.CompareTo(b.count);
            if (result != 0)
            {
                return result;
            }

            return String.Compare(a.name, b.name, StringComparison.Ordinal);
        }
    }
}