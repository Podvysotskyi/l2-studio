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
}
