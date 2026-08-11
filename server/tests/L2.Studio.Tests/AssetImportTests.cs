using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace L2.Studio.Tests;

public sealed class AssetImportTests
{
    [Theory]
    [InlineData("17_25.unr", true)]
    [InlineData("skylevel.unr", false)]
    [InlineData("17_250.unr", false)]
    public void RoutesUnrealPackagesByCoordinateName(string fileName, bool isLevel)
    {
        Assert.Equal(isLevel, UnrealPackageKindClassifier.IsWorldLevel(fileName));
        Assert.Equal(!isLevel, UnrealPackageKindClassifier.IsScene(fileName));
    }

    [Fact]
    public void DefinesTypedDiscoveryAndFileCommandsForEverySupportedKind()
    {
        var assembly = typeof(DiscoverTextures).Assembly;
        Assert.Equal(8, assembly.GetTypes().Count(type =>
            !type.IsInterface && typeof(IAssetImportDiscoveryCommand).IsAssignableFrom(type)));
        Assert.Equal(8, assembly.GetTypes().Count(type =>
            !type.IsInterface && typeof(IAssetImportFileCommand).IsAssignableFrom(type)));
    }

    [Fact]
    public void AggregatesRunCountsAndWarningsByTerminalFile()
    {
        var run = Run(
            Item(AssetImportJobValues.Succeeded),
            Item(AssetImportJobValues.SucceededWithWarnings, warnings: 2),
            Item(AssetImportJobValues.Failed),
            Item(AssetImportJobValues.Running));

        AssetImportRunHandlers.ApplyCounts(run);

        Assert.Equal(3, run.CompletedFileCount);
        Assert.Equal(2, run.SucceededFileCount);
        Assert.Equal(1, run.WarningFileCount);
        Assert.Equal(1, run.FailedFileCount);
    }

    [Fact]
    public void MergesPerSourceTextureMetadata()
    {
        var result = AssetCatalogMetadataAggregator.Aggregate(AssetImportJobValues.Textures,
        [
            "{\"materials\":[{\"objectName\":\"A\"}]}",
            "{\"materials\":[{\"objectName\":\"B\"}]}"
        ]);
        using var json = JsonDocument.Parse(result);
        Assert.Equal(2, json.RootElement.GetProperty("materials").GetArrayLength());
    }

    [Fact]
    public async Task HashesFilesAndDerivesStablePreviewHashes()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllTextAsync(path, "source bytes");
            Assert.Equal(64, (await AssetImportSourceHash.FileAsync(path, CancellationToken.None)).Length);
            Assert.Equal(
                AssetImportSourceHash.LevelPreview("abc"),
                AssetImportSourceHash.LevelPreview("abc"));
            Assert.NotEqual(
                AssetImportSourceHash.LevelPreview("abc"),
                AssetImportSourceHash.LevelPreview("def"));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ModelEnforcesActiveRunAndRunSourceUniqueness()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql("Host=localhost;Database=model;Username=model;Password=model")
            .Options;
        using var context = new GameContentDbContext(options);
        var runIndexes = context.Model.FindEntityType(typeof(AssetImportRun))!.GetIndexes();
        Assert.Contains(runIndexes, index => index.IsUnique &&
            index.GetFilter()!.Contains("full_scan", StringComparison.Ordinal));
        var itemIndexes = context.Model.FindEntityType(typeof(AssetImportWorkItem))!.GetIndexes();
        Assert.Contains(itemIndexes, index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(AssetImportWorkItem.RunId), nameof(AssetImportWorkItem.NormalizedSourceKey)]));
    }

    [Fact]
    public void ResolvesOnlyAContainedFileWithTheExpectedExtension()
    {
        var root = Path.Combine(Path.GetTempPath(), $"l2-import-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var source = Path.Combine(root, "Example.UTX");
            File.WriteAllText(source, "test");
            Assert.Equal(source, AssetImportPathValidator.ResolveContainedFile(root, "example.utx", ".utx"));
            Assert.Throws<ArgumentException>(() =>
                AssetImportPathValidator.ResolveContainedFile(root, "../Example.UTX", ".utx"));
            Assert.Throws<ArgumentException>(() =>
                AssetImportPathValidator.ResolveContainedFile(root, "Example.UTX", ".usx"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static AssetImportRun Run(params AssetImportWorkItem[] workItems) => new()
    {
        Id = Guid.NewGuid(),
        Kind = AssetImportJobValues.Textures,
        TriggerType = AssetImportJobValues.FullScan,
        Status = AssetImportJobValues.Running,
        RequestedAt = DateTimeOffset.UtcNow,
        WorkItems = workItems
    };

    private static AssetImportWorkItem Item(string status, int warnings = 0) => new()
    {
        Id = Guid.NewGuid(),
        ImportKind = AssetImportJobValues.Textures,
        SourceKey = $"{Guid.NewGuid():N}.utx",
        NormalizedSourceKey = Guid.NewGuid().ToString("N"),
        SourcePath = "/tmp/source.utx",
        Status = status,
        WarningCount = warnings,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
