namespace UProfiler.Server.Services;

public static class ChartDataDownsampler
{
    public const int DefaultMaxPoints = 1500;

    public static IReadOnlyList<int> PickIndices(int count, int maxPoints = DefaultMaxPoints)
    {
        if (count <= 0)
        {
            return Array.Empty<int>();
        }

        if (count <= maxPoints)
        {
            return Enumerable.Range(0, count).ToArray();
        }

        var indices = new int[maxPoints];
        var step = (count - 1) / (double)(maxPoints - 1);
        for (var i = 0; i < maxPoints; i++)
        {
            indices[i] = (int)Math.Round(i * step);
        }

        indices[^1] = count - 1;
        return indices;
    }

    public static T[] Downsample<T>(IReadOnlyList<T> source, int maxPoints = DefaultMaxPoints)
    {
        if (source.Count == 0)
        {
            return Array.Empty<T>();
        }

        var indices = PickIndices(source.Count, maxPoints);
        var result = new T[indices.Count];
        for (var i = 0; i < indices.Count; i++)
        {
            result[i] = source[indices[i]];
        }

        return result;
    }

    public static T[] DownsampleByIndices<T>(IReadOnlyList<T> source, IReadOnlyList<int> indices)
    {
        if (source.Count == 0 || indices.Count == 0)
        {
            return Array.Empty<T>();
        }

        var result = new T[indices.Count];
        for (var i = 0; i < indices.Count; i++)
        {
            var index = indices[i];
            result[i] = source[Math.Clamp(index, 0, source.Count - 1)];
        }

        return result;
    }
}
