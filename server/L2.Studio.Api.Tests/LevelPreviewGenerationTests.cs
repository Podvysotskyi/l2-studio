using L2.Studio.Worker;
using Xunit;

namespace L2.Foundation.Tests;

public sealed class LevelPreviewGenerationTests
{
    [Fact]
    public void Matching_resolved_preview_is_reused()
    {
        var level = Level("16_25", "level-hash");
        var preview = Preview("16_25", "level-hash", "resolved", "/levelpreviews/16_25.webp?v=image");
        var catalog = Catalog(LevelPreviewGeneration.RendererVersion, preview);

        Assert.True(LevelPreviewGeneration.CanReuse(catalog, preview, level, imageExists: true));
        Assert.False(LevelPreviewGeneration.CanReuse(
            catalog,
            preview,
            level,
            imageExists: true,
            force: true));
    }

    [Theory]
    [InlineData("different-hash", "resolved", true)]
    [InlineData("level-hash", "skipped", true)]
    [InlineData("level-hash", "resolved", false)]
    public void Changed_failed_or_missing_preview_is_not_reused(
        string previewHash,
        string status,
        bool imageExists)
    {
        var level = Level("16_25", "level-hash");
        var imageUrl = status == "resolved" ? "/levelpreviews/16_25.webp?v=image" : null;
        var preview = Preview("16_25", previewHash, status, imageUrl);

        Assert.False(LevelPreviewGeneration.CanReuse(
            Catalog(LevelPreviewGeneration.RendererVersion, preview),
            preview,
            level,
            imageExists));
    }

    [Fact]
    public void Renderer_version_participates_in_the_catalog_hash_and_reuse_decision()
    {
        var level = Level("16_25", "level-hash");
        var preview = Preview("16_25", "level-hash", "resolved", "/levelpreviews/16_25.webp?v=image");

        Assert.Equal(64, LevelPreviewGeneration.ComputeSourceHash("catalog-hash").Length);
        Assert.False(LevelPreviewGeneration.CanReuse(
            Catalog(LevelPreviewGeneration.RendererVersion + 1, preview),
            preview,
            level,
            imageExists: true));
    }

    [Fact]
    public void Targeted_job_name_is_encoded_by_a_level_file_beneath_the_source_directory()
    {
        var source = Path.Combine(Path.GetTempPath(), "l2-level-preview-tests", "maps");

        Assert.Null(LevelPreviewGeneration.RequestedLevelName(source, source));
        Assert.Equal(
            "16_25",
            LevelPreviewGeneration.RequestedLevelName(source, Path.Combine(source, "16_25.unr")));
        Assert.Throws<InvalidOperationException>(() => LevelPreviewGeneration.RequestedLevelName(
            source,
            Path.Combine(Path.GetDirectoryName(source)!, "16_25.unr")));
    }

    [Theory]
    [InlineData("resolved", true, true)]
    [InlineData("resolved", false, false)]
    [InlineData("skipped", false, true)]
    public void Targeted_job_carries_forward_valid_untouched_entries(
        string status,
        bool imageExists,
        bool expected)
    {
        var preview = Preview(
            "16_24",
            "level-hash",
            status,
            status == "resolved" ? "/levelpreviews/16_24.webp?v=image" : null);

        Assert.Equal(expected, LevelPreviewGeneration.CanCarryForward(preview, imageExists));
    }

    private static LevelCatalogEntry Level(string name, string hash) =>
        new(name, $"{name}.unr", $"/levels/{name}/manifest.json", 1, 1, 0, hash, "resolved", null);

    private static LevelPreviewCatalogEntry Preview(
        string name,
        string hash,
        string status,
        string? imageUrl) =>
        new(name, hash, imageUrl, 512, 512, status, status == "resolved" ? null : "failed");

    private static LevelPreviewCatalogManifest Catalog(
        int rendererVersion,
        params LevelPreviewCatalogEntry[] previews) =>
        new(1, "levelpreviews", "source", rendererVersion, previews);
}
