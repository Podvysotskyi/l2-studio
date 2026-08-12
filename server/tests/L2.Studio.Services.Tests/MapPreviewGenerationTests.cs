using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class MapPreviewGenerationTests
{
    [Fact]
    public void TreatsTheConfiguredMapDirectoryAsAFullPreviewRun()
    {
        var root = Path.Combine(Path.GetTempPath(), $"l2-maps-{Guid.NewGuid():N}");

        Assert.Null(MapPreviewGeneration.RequestedMapName(root, root));
        Assert.Null(MapPreviewGeneration.RequestedMapName(
            root + Path.DirectorySeparatorChar,
            root));
    }

    [Fact]
    public void ExtractsTheNameOfATargetedWorldMap()
    {
        var root = Path.Combine(Path.GetTempPath(), $"l2-maps-{Guid.NewGuid():N}");
        var source = Path.Combine(root, "17_25.UNR");

        Assert.Equal("17_25", MapPreviewGeneration.RequestedMapName(root, source));
    }

    [Fact]
    public void RejectsInvalidTargetedMapPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), $"l2-maps-{Guid.NewGuid():N}");
        foreach (var relativePath in new[]
                 {
                     "nested/17_25.unr",
                     "17_25.txt",
                     "../17_25.unr"
                 })
        {
            var source = Path.Combine(root, relativePath);
            var exception = Assert.Throws<InvalidOperationException>(() =>
                MapPreviewGeneration.RequestedMapName(root, source));
            Assert.Contains(".unr file", exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReusesOnlyAMatchingResolvedPreview()
    {
        var map = Map("map-hash");
        var entry = Preview("map-hash", "resolved", "/assets/17_25.webp");
        var previous = Manifest(MapPreviewGeneration.RendererVersion, entry);

        Assert.True(MapPreviewGeneration.CanReuse(previous, entry, map, imageExists: true));
        Assert.False(MapPreviewGeneration.CanReuse(previous, entry, map, imageExists: true, force: true));
        Assert.False(MapPreviewGeneration.CanReuse(
            Manifest(MapPreviewGeneration.RendererVersion - 1, entry),
            entry,
            map,
            imageExists: true));
        Assert.False(MapPreviewGeneration.CanReuse(
            previous,
            Preview("other-hash", "resolved", "/assets/17_25.webp"),
            map,
            imageExists: true));
        Assert.False(MapPreviewGeneration.CanReuse(
            previous,
            Preview("map-hash", "failed", null),
            map,
            imageExists: true));
        Assert.False(MapPreviewGeneration.CanReuse(previous, entry, map, imageExists: false));
    }

    [Fact]
    public void CarriesFailuresForwardButRequiresResolvedImagesToExist()
    {
        Assert.True(MapPreviewGeneration.CanCarryForward(
            Preview("map-hash", "failed", null),
            imageExists: false));
        Assert.True(MapPreviewGeneration.CanCarryForward(
            Preview("map-hash", "resolved", "/assets/17_25.webp"),
            imageExists: true));
        Assert.False(MapPreviewGeneration.CanCarryForward(
            Preview("map-hash", "resolved", "/assets/17_25.webp"),
            imageExists: false));
        Assert.False(MapPreviewGeneration.CanCarryForward(
            Preview("map-hash", "resolved", null),
            imageExists: true));
        Assert.False(MapPreviewGeneration.CanCarryForward(null, imageExists: true));
    }

    [Fact]
    public void DerivesPreviewHashesUsingThePublishedRendererVersion()
    {
        Assert.Equal(
            AssetImportSourceHash.MapPreview("map-hash"),
            MapPreviewGeneration.ComputeSourceHash("map-hash"));
        Assert.Equal(
            AssetImportSourceHash.MapPreviewRendererVersion,
            MapPreviewGeneration.RendererVersion);
    }

    private static MapCatalogEntry Map(string hash) => new(
        "17_25", "17_25.unr", null, 1, 2, 3, hash, "resolved", null);

    private static MapPreviewCatalogEntry Preview(
        string hash,
        string status,
        string? imageUrl) => new(
        "17_25", hash, imageUrl, 512, 512, status, null);

    private static MapPreviewCatalogManifest Manifest(
        int rendererVersion,
        params MapPreviewCatalogEntry[] previews) => new(
        1, "mappreviews", "source-hash", rendererVersion, previews);
}
