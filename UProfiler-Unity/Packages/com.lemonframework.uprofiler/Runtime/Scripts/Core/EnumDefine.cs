namespace LemonFramework.UProfiler.Core
{
    /// <summary>Function analysis inject mode.</summary>
    public enum EAnalyzeType
    {
        /// <summary>All methods.</summary>
        ALLFUNC = 0,
        /// <summary>Methods marked with [FunctionAnalysis].</summary>
        DEFINEFUNC,
        /// <summary>Methods marked with [ProfilerSample].</summary>
        PROFILESAMPLE
    }

    /// <summary>Hook wrapper type for Begin/End injection.</summary>
    public enum EBeginEndType
    {
        /// <summary>Debug.Log at entry/exit.</summary>
        LOG = 1,
        /// <summary>Performance timing.</summary>
        ANALYZE
    }
}
