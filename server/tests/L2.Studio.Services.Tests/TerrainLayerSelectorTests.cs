using L2.Studio.Services;
using L2.Tools.PackageReader;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class TerrainLayerSelectorTests
{
    [Fact]
    public void SelectsAllCompleteLayers()
    {
        var layers = new[] { Layer(0), Layer(1), Layer(2) };

        var result = TerrainLayerSelector.SelectCompletePrefix(layers);

        Assert.Equal(layers, result.Layers);
        Assert.Empty(result.IgnoredTrailingLayerIndices);
        Assert.Null(result.Error);
    }

    [Fact]
    public void IgnoresIncompleteLayersAfterTheLastCompleteLayer()
    {
        var complete = Layer(0);
        var result = TerrainLayerSelector.SelectCompletePrefix(
            [complete, Layer(1, alphaMap: false), Layer(2, texture: false)]);

        Assert.Equal([complete], result.Layers);
        Assert.Equal([1, 2], result.IgnoredTrailingLayerIndices);
        Assert.Null(result.Error);
    }

    [Fact]
    public void RejectsAnIncompleteLayerBeforeALaterCompleteLayer()
    {
        var result = TerrainLayerSelector.SelectCompletePrefix(
            [Layer(0), Layer(4, texture: false), Layer(7)]);

        Assert.Empty(result.Layers);
        Assert.Empty(result.IgnoredTrailingLayerIndices);
        Assert.Equal(
            "Terrain has an incomplete texture layer before a later complete layer: 4.",
            result.Error);
    }

    [Fact]
    public void ReportsWhenNoCompleteLayerExists()
    {
        var result = TerrainLayerSelector.SelectCompletePrefix(
            [Layer(3, texture: false), Layer(8, alphaMap: false)]);

        Assert.Empty(result.Layers);
        Assert.Empty(result.IgnoredTrailingLayerIndices);
        Assert.Equal("Terrain has no complete texture layers.", result.Error);
    }

    [Fact]
    public void ReportsWhenTheLayerCollectionIsEmpty()
    {
        var result = TerrainLayerSelector.SelectCompletePrefix([]);

        Assert.Empty(result.Layers);
        Assert.Equal("Terrain has no complete texture layers.", result.Error);
    }

    [Fact]
    public void RejectsANullLayerCollection()
    {
        Assert.Throws<ArgumentNullException>(() =>
            TerrainLayerSelector.SelectCompletePrefix(null!));
    }

    private static UnrealTerrainLayer Layer(
        int index,
        bool texture = true,
        bool alphaMap = true) => new(
        index,
        texture ? Reference($"Texture{index}") : null,
        alphaMap ? Reference($"Alpha{index}") : null,
        1,
        1,
        0,
        0,
        0,
        0,
        new UnrealRotator(0, 0, 0));

    private static UnrealObjectReference Reference(string name) => new(
        "Package", name, "Texture");
}
