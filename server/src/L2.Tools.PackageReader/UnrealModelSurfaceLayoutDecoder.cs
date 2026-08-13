namespace L2.Tools.PackageReader;

internal static class UnrealModelSurfaceLayoutDecoder
{
    internal const int LineageSurfaceBytes = 4;
    internal const int StockSurfaceBytes = 0;

    internal static T Decode<T>(Func<int, T> decode)
    {
        ArgumentNullException.ThrowIfNull(decode);
        Exception? firstError = null;
        foreach (var lineageSurfaceBytes in new[] { LineageSurfaceBytes, StockSurfaceBytes })
        {
            try
            {
                return decode(lineageSurfaceBytes);
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
            {
                firstError ??= exception;
            }
        }

        throw firstError!;
    }

    internal static T DecodeBest<T>(Func<int, T> decode, Comparison<T> compare)
    {
        ArgumentNullException.ThrowIfNull(decode);
        ArgumentNullException.ThrowIfNull(compare);
        Exception? firstError = null;
        var hasBest = false;
        var best = default(T)!;
        foreach (var lineageSurfaceBytes in new[] { LineageSurfaceBytes, StockSurfaceBytes })
        {
            try
            {
                var candidate = decode(lineageSurfaceBytes);
                if (!hasBest || compare(candidate, best) > 0)
                {
                    best = candidate;
                    hasBest = true;
                }
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
            {
                firstError ??= exception;
            }
        }

        return hasBest ? best : throw firstError!;
    }
}
