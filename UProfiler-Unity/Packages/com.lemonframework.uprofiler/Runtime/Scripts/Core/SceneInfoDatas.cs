using System;
using System.Collections.Generic;

namespace LemonFramework.UProfiler.Core
{
    [Serializable]
    public class SceneSegmentData
    {
        public string sceneName = "";
        public int startFrame;
        public int endFrame;
        public string note = "";
    }

    [Serializable]
    public class SceneInfoData
    {
        public List<SceneSegmentData> segments = new List<SceneSegmentData>();
    }
}
