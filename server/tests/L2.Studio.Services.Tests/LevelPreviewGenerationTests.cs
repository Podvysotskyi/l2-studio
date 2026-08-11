using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class LevelPreviewGenerationTests
{
    [Fact]
    public void TreatsTheConfiguredLevelDirectoryAsAFullPreviewRun()
    {
        var root = Path.Combine(Path.GetTempPath(), $"l2-levels-{Guid.NewGuid():N}");

        Assert.Null(LevelPreviewGeneration.RequestedLevelName(root, root));
        Assert.Null(LevelPreviewGeneration.RequestedLevelName(
            root + Path.DirectorySeparatorChar,
            root));
    }

    [Fact]
    public void ExtractsTheNameOfATargetedWorldLevel()
    {
        var root = Path.Combine(Path.GetTempPath(), $"l2-levels-{Guid.NewGuid():N}");
        var source = Path.Combine(root, "17_25.UNR");

        Assert.Equal("17_25", LevelPreviewGeneration.RequestedLevelName(root, source));
    }

    [Fact]
    public void RejectsInvalidTargetedLevelPaths()
    {
        var root = Path.Combine(Path.GetTempPath(), $"l2-levels-{Guid.NewGuid():N}");
        foreach (var relativePath in new[]
                 {
                     "nested/17_25.unr",
                     "17_25.txt",
                     "../17_25.unr"
                 })
        {
            var source = Path.Combine(root, relativePath);
            var exception = Assert.Throws<InvalidOperationException>(() =>
                LevelPreviewGeneration.RequestedLevelName(root, source));
            Assert.Contains(".unr file", exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void ReusesOnlyAMatchingResolvedPreview()
    {
        var level = Level("level-hash");
        var entry = Preview("level-hash", "resolved", "/assets/17_25.webp");
        var previous = Manifest(LevelPreviewGeneration.RendererVersion, entry);

        Assert.True(LevelPreviewGeneration.CanReuse(previous, entry, level, imageExists: true));
        Assert.False(LevelPreviewGeneration.CanReuse(previous, entry, level, imageExists: true, force: true));
        Assert.False(LevelPreviewGeneration.CanReuse(
            Manifest(LevelPreviewGeneration.RendererVersion - 1, entry),
            entry,
            level,
            imageExists: true));
        Assert.False(LevelPreviewGeneration.CanReuse(
            previous,
            Preview("other-hash", "resolved", "/assets/17_25.webp"),
            level,
            imageExists: true));
        Assert.False(LevelPreviewGeneration.CanReuse(
            previous,
            Preview("level-hash", "failed", null),
            level,
            imageExists: true));
        Assert.False(LevelPreviewGeneration.CanReuse(previous, entry, level, imageExists: false));
    }

    [Fact]
    public void CarriesFailuresForwardButRequiresResolvedImagesToExist()
    {
        Assert.True(LevelPreviewGeneration.CanCarryForward(
            Preview("level-hash", "failed", null),
            imageExists: false));
        Assert.True(LevelPreviewGeneration.CanCarryForward(
            Preview("level-hash", "resolved", "/assets/17_25.webp"),
            imageExists: true));
        Assert.False(LevelPreviewGeneration.CanCarryForward(
            Preview("level-hash", "resolved", "/assets/17_25.webp"),
            imageExists: false));
        Assert.False(LevelPreviewGeneration.CanCarryForward(
            Preview("level-hash", "resolved", null),
            imageExists: true));
        Assert.False(LevelPreviewGeneration.CanCarryForward(null, imageExists: true));
    }

    [Fact]
    public void DerivesPreviewHashesUsingThePublishedRendererVersion()
    {
        Assert.Equal(
            AssetImportSourceHash.LevelPreview("level-hash"),
            LevelPreviewGeneration.ComputeSourceHash("level-hash"));
        Assert.Equal(
            AssetImportSourceHash.LevelPreviewRendererVersion,
            LevelPreviewGeneration.RendererVersion);
    }

    private static LevelCatalogEntry Level(string hash) => new(
        "17_25", "17_25.unr", null, 1, 2, 3, hash, "resolved", null);

    private static LevelPreviewCatalogEntry Preview(
        string hash,
        string status,
        string? imageUrl) => new(
        "17_25", hash, imageUrl, 512, 512, status, null);

    private static LevelPreviewCatalogManifest Manifest(
        int rendererVersion,
        params LevelPreviewCatalogEntry[] previews) => new(
        1, "levelpreviews", "source-hash", rendererVersion, previews);
}
