using System.Text.Json;
using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Repositories.Interfaces.Tests;

public sealed class AssetCatalogMetadataAggregatorTests
{
    [Fact]
    public void ReturnsEmptyObjectWhenNoSourceMetadataExists()
    {
        Assert.Equal("{}", AssetCatalogMetadataAggregator.Aggregate(
            AssetImportJobValues.Music,
            []));
    }

    [Fact]
    public void MergesTextureMaterialsInSourceOrder()
    {
        {
            var result = AssetCatalogMetadataAggregator.Aggregate(AssetImportJobValues.Textures,
            [
                "{\"materials\":[{\"objectName\":\"A\"}]}",
                "{}",
                "{\"materials\":[{\"objectName\":\"B\"},{\"objectName\":\"C\"}]}"
            ]);

            using var json = JsonDocument.Parse(result);
            var names = json.RootElement.GetProperty("materials")
                .EnumerateArray()
                .Select(item => item.GetProperty("objectName").GetString()!)
                .ToArray();
            Assert.Equal(["A", "B", "C"], names);
        }
    }

    [Fact]
    public void SortsAndDeduplicatesStaticMeshTextureFormats()
    {
        var result = AssetCatalogMetadataAggregator.Aggregate(
            AssetImportJobValues.StaticMeshes,
        [
            "{\"gpuTextureFormats\":[\"webp\",\"ktx2\"]}",
            "{\"gpuTextureFormats\":[\"ktx2\",\"dds\"]}",
            "{}"
        ]);

        using var json = JsonDocument.Parse(result);
        var formats = json.RootElement.GetProperty("gpuTextureFormats")
            .EnumerateArray()
            .Select(item => item.GetString()!)
            .ToArray();
        Assert.Equal(["dds", "ktx2", "webp"], formats);
    }

    [Fact]
    public void UsesOldestRendererVersionAcrossMapPreviews()
    {
        var result = AssetCatalogMetadataAggregator.Aggregate(
            AssetImportJobValues.MapPreviews,
        [
            "{\"rendererVersion\":5}",
            "{\"rendererVersion\":3}",
            "{}"
        ]);

        using var json = JsonDocument.Parse(result);
        Assert.Equal(0, json.RootElement.GetProperty("rendererVersion").GetInt32());
    }

    [Fact]
    public void UsesLastSourceMetadataForKindsWithoutSpecialAggregation()
    {
        var result = AssetCatalogMetadataAggregator.Aggregate(
            AssetImportJobValues.Music,
        [
            "{\"trackCount\":1}",
            "{\"trackCount\":2,\"codec\":\"vorbis\"}"
        ]);

        using var json = JsonDocument.Parse(result);
        Assert.Equal(2, json.RootElement.GetProperty("trackCount").GetInt32());
        Assert.Equal("vorbis", json.RootElement.GetProperty("codec").GetString());
    }
}
