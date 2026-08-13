using L2.Tools.PackageReader;
using Xunit;

namespace L2.Tools.PackageReader.Tests;

public sealed class UnrealModelSurfaceLayoutDecoderTests
{
    [Fact]
    public void PrefersTheLineageSurfaceExtension()
    {
        var layouts = new List<int>();

        var result = UnrealModelSurfaceLayoutDecoder.Decode(layout =>
        {
            layouts.Add(layout);
            return "decoded";
        });

        Assert.Equal("decoded", result);
        Assert.Equal([UnrealModelSurfaceLayoutDecoder.LineageSurfaceBytes], layouts);
    }

    [Fact]
    public void FallsBackToTheStockSurfaceLayout()
    {
        var layouts = new List<int>();

        var result = UnrealModelSurfaceLayoutDecoder.Decode(layout =>
        {
            layouts.Add(layout);
            if (layout == UnrealModelSurfaceLayoutDecoder.LineageSurfaceBytes)
                throw new InvalidDataException("extended layout does not match");
            return "decoded";
        });

        Assert.Equal("decoded", result);
        Assert.Equal(
            [UnrealModelSurfaceLayoutDecoder.LineageSurfaceBytes, UnrealModelSurfaceLayoutDecoder.StockSurfaceBytes],
            layouts);
    }

    [Fact]
    public void PreservesThePreferredLayoutFailureWhenNeitherLayoutMatches()
    {
        var preferred = new InvalidDataException("preferred layout failure");

        var exception = Assert.Throws<InvalidDataException>(() =>
            UnrealModelSurfaceLayoutDecoder.Decode<string>(layout =>
            {
                if (layout == UnrealModelSurfaceLayoutDecoder.LineageSurfaceBytes) throw preferred;
                throw new OverflowException("stock layout failure");
            }));

        Assert.Same(preferred, exception);
    }

    [Fact]
    public void SelectsTheHigherScoringLayoutWhenBothDecode()
    {
        var layouts = new List<int>();

        var result = UnrealModelSurfaceLayoutDecoder.DecodeBest(
            layout =>
            {
                layouts.Add(layout);
                return layout;
            },
            (left, right) => left == UnrealModelSurfaceLayoutDecoder.StockSurfaceBytes ? 1 : -1);

        Assert.Equal(UnrealModelSurfaceLayoutDecoder.StockSurfaceBytes, result);
        Assert.Equal(
            [UnrealModelSurfaceLayoutDecoder.LineageSurfaceBytes, UnrealModelSurfaceLayoutDecoder.StockSurfaceBytes],
            layouts);
    }

    [Fact]
    public void RetainsThePreferredLayoutWhenScoresTie()
    {
        var result = UnrealModelSurfaceLayoutDecoder.DecodeBest(
            layout => layout,
            (_, _) => 0);

        Assert.Equal(UnrealModelSurfaceLayoutDecoder.LineageSurfaceBytes, result);
    }

    [Fact]
    public void SelectsTheOnlyLayoutThatDecodesDuringScoring()
    {
        var result = UnrealModelSurfaceLayoutDecoder.DecodeBest(
            layout => layout == UnrealModelSurfaceLayoutDecoder.LineageSurfaceBytes
                ? throw new InvalidDataException("extended layout does not match")
                : layout,
            Comparer<int>.Default.Compare);

        Assert.Equal(UnrealModelSurfaceLayoutDecoder.StockSurfaceBytes, result);
    }
}
