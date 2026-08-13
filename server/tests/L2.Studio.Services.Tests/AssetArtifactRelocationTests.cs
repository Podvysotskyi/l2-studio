using L2.Studio.Context.Entities;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using L2.Studio.Services.Interfaces;
using Microsoft.Extensions.Options;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class AssetArtifactRelocationTests
{
    [Fact]
    public async Task RelocationRewritesCatalogAndInventoryUrlsToTheFinalFingerprint()
    {
        var assetRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        const string provisionalUrlRoot = "versions/c1/Maps/Lobby/provisional";
        const string finalFingerprint = "final";
        var provisionalPath = Path.Combine(assetRoot, provisionalUrlRoot);
        Directory.CreateDirectory(provisionalPath);
        try
        {
            var manifestUrl = $"/{provisionalUrlRoot}/Lobby/manifest.json";
            var manifestPath = Path.Combine(provisionalPath, "Lobby", "manifest.json");
            Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
            await File.WriteAllTextAsync(manifestPath, $"{{\"manifestUrl\":\"{manifestUrl}\"}}");
            var processor = new AssetImportJobProcessor(
                null!,
                null!,
                null!,
                Options.Create(new AssetImportOptions { AssetRootPath = assetRoot }),
                TimeProvider.System,
                null!);
            var job = new AssetImportWorkItem
            {
                GameVersion = "c1",
                ImportKind = AssetImportJobValues.Scenes,
                SourceKey = "Maps/Lobby.unr",
                NormalizedSourceKey = "maps/lobby.unr",
                SourcePath = "Lobby.unr",
                Status = AssetImportJobValues.Running
            };
            var entries = new[]
            {
                new AssetCatalogPublicationEntry("Lobby", null, "resolved", $"{{\"manifestUrl\":\"{manifestUrl}\"}}")
            };

            var relocated = processor.RelocateArtifact(
                job, provisionalPath, provisionalUrlRoot, finalFingerprint, [], entries, "{}");
            var expectedUrlRoot = "versions/c1/Maps/Lobby/final";
            var expectedManifestUrl = $"/{expectedUrlRoot}/Lobby/manifest.json";
            var files = await AssetImportJobProcessor.InventoryFilesAsync(
                relocated.FinalPath, relocated.PublishedUrlRoot, CancellationToken.None);

            Assert.Equal(expectedUrlRoot, relocated.PublishedUrlRoot);
            Assert.Contains(expectedManifestUrl, relocated.Items.Single().MetadataJson, StringComparison.Ordinal);
            Assert.Equal($"{{\"manifestUrl\":\"{expectedManifestUrl}\"}}", await File.ReadAllTextAsync(
                Path.Combine(relocated.FinalPath, "Lobby", "manifest.json")));
            Assert.Equal(expectedManifestUrl, files.Single().PublicPath);
        }
        finally
        {
            if (Directory.Exists(assetRoot)) Directory.Delete(assetRoot, recursive: true);
        }
    }
}
