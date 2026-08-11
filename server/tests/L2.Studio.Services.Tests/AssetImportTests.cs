using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace L2.Studio.Services.Tests;

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
    public void ResetsRunCountsWhenNoWorkItemsExist()
    {
        var run = Run();
        run.CompletedFileCount = 10;
        run.SucceededFileCount = 9;
        run.WarningFileCount = 8;
        run.FailedFileCount = 7;

        AssetImportRunHandlers.ApplyCounts(run);

        Assert.Equal(0, run.CompletedFileCount);
        Assert.Equal(0, run.SucceededFileCount);
        Assert.Equal(0, run.WarningFileCount);
        Assert.Equal(0, run.FailedFileCount);
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
