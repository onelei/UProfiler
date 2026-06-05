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
}
