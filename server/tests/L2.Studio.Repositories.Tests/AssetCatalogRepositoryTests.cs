using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace L2.Studio.Repositories.Tests;

public sealed class AssetCatalogRepositoryTests
{
    [Fact]
    public async Task ReturnsTheRequestedActiveNpcAppearanceManifestReference()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            context.AssetCatalogs.Add(new AssetCatalog
            {
                Id = Guid.NewGuid(),
                GameVersion = "c1",
                Kind = "npcappearances",
                SourceFolder = "system",
                SourceHash = "catalog-hash",
                SchemaVersion = 6,
                MetadataJson = """
                    {"npcManifestUrlTemplate":"/versions/c1/system/npcgrp/npcs/{id}/manifest.json","npcIds":[7],"npcCount":7,"resolvedReferenceCount":20,"unresolvedReferenceCount":3}
                    """,
                IsActive = true,
                PublishedAt = DateTimeOffset.UnixEpoch
            });
            await context.SaveChangesAsync();
        }
        var repository = new AssetCatalogRepository(
            new TestContextFactory(options),
            Options.Create(new AssetImportOptions()),
            TimeProvider.System);

        var result = await repository.GetNpcAppearanceManifestAsync("c1", 7, CancellationToken.None);

        Assert.Equal("/versions/c1/system/npcgrp/npcs/7/manifest.json", result?.ManifestUrl);

        Assert.Null(await repository.GetNpcAppearanceManifestAsync("c1", 8, CancellationToken.None));
    }

    [Fact]
    public async Task IgnoresLegacyNpcAppearanceIndexesWithoutMaterialSlots()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            context.AssetCatalogs.Add(new AssetCatalog
            {
                Id = Guid.NewGuid(),
                GameVersion = "c1",
                Kind = "npcappearances",
                SourceFolder = "system",
                SourceHash = "legacy-hash",
                SchemaVersion = 5,
                MetadataJson = "{\"npcManifestUrlTemplate\":\"/legacy/npcs/{id}/manifest.json\",\"npcIds\":[501]}",
                IsActive = true,
                PublishedAt = DateTimeOffset.UnixEpoch
            });
            await context.SaveChangesAsync();
        }
        var repository = new AssetCatalogRepository(
            new TestContextFactory(options),
            Options.Create(new AssetImportOptions()),
            TimeProvider.System);

        Assert.Null(await repository.GetNpcAppearanceManifestAsync("c1", 501, CancellationToken.None));
    }

    [Fact]
    public async Task ReturnsDiagnosticsFromTheWorkItemThatPublishedTheDisplayedArtifact()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var publishingRunId = Guid.NewGuid();
        var publishingWorkItemId = Guid.NewGuid();
        var newerRunId = Guid.NewGuid();
        var newerWorkItemId = Guid.NewGuid();
        var publishedAt = DateTimeOffset.UtcNow.AddMinutes(-5);
        await using (var context = new GameContentDbContext(options))
        {
            var catalog = new AssetCatalog
            {
                Id = Guid.NewGuid(),
                GameVersion = "c1",
                Kind = "maps",
                SourceFolder = "Maps",
                SourceHash = "catalog-hash",
                MetadataJson = "{}",
                IsActive = true,
                PublishedAt = publishedAt
            };
            var source = new AssetCatalogSource
            {
                Id = Guid.NewGuid(),
                Catalog = catalog,
                ArtifactId = Guid.NewGuid(),
                PublishingWorkItemId = publishingWorkItemId,
                SourceKey = "Maps/16_25.unr",
                NormalizedSourceKey = "maps/16_25.unr",
                SourceHash = "source-hash",
                OutputRoot = "versions/c1/Maps/16_25/fingerprint",
                MetadataJson = "{}",
                ReferencedOutputRootsJson = "[]",
                PublishedAt = publishedAt
            };
            context.AssetCatalogItems.Add(new AssetCatalogItem
            {
                Catalog = catalog,
                Source = source,
                Name = "16_25",
                Status = "resolved",
                MetadataJson = "{}"
            });
            context.AssetImportRuns.AddRange(
                Run(publishingRunId, publishedAt),
                Run(newerRunId, publishedAt.AddMinutes(4)));
            context.AssetImportWorkItems.AddRange(
                WorkItem(publishingWorkItemId, publishingRunId),
                WorkItem(newerWorkItemId, newerRunId));
            context.AssetImportDiagnostics.AddRange(
                Diagnostic(publishingRunId, publishingWorkItemId, "Displayed artifact warning", publishedAt),
                Diagnostic(newerRunId, newerWorkItemId, "Newer attempt warning", publishedAt.AddMinutes(4)));
            await context.SaveChangesAsync();
        }
        var repository = new AssetCatalogRepository(
            new TestContextFactory(options),
            Options.Create(new AssetImportOptions()),
            TimeProvider.System);

        var result = await repository.GetDiagnosticsAsync(
            "c1", "maps", "16_25", "Maps/16_25.unr", null, null, 1, 25, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(publishingRunId, result.RunId);
        Assert.Equal(publishingWorkItemId, result.WorkItemId);
        Assert.Equal(publishedAt, result.PublishedAt);
        var diagnostic = Assert.Single(result.Items);
        Assert.Equal("Displayed artifact warning", diagnostic.Message);
    }

    private static AssetImportRun Run(Guid id, DateTimeOffset requestedAt) => new()
    {
        Id = id,
        GameVersion = "c1",
        Kind = "maps",
        TriggerType = "single_file",
        Status = "succeeded_with_warnings",
        RequestedAt = requestedAt
    };

    private static AssetImportWorkItem WorkItem(Guid id, Guid runId) => new()
    {
        Id = id,
        RunId = runId,
        GameVersion = "c1",
        ImportKind = "maps",
        SourceKey = "Maps/16_25.unr",
        NormalizedSourceKey = "maps/16_25.unr",
        SourcePath = "/workspace/sources/C1/Maps/16_25.unr",
        Status = "succeeded_with_warnings"
    };

    private static AssetImportDiagnostic Diagnostic(
        Guid runId,
        Guid workItemId,
        string message,
        DateTimeOffset createdAt) => new()
    {
        RunId = runId,
        WorkItemId = workItemId,
        Severity = "warning",
        Code = "map.resource_warning",
        Stage = "conversion",
        SourceKey = "Maps/16_25.unr",
        Message = message,
        CreatedAt = createdAt
    };

    private sealed class TestContextFactory(DbContextOptions<GameContentDbContext> options)
        : IDbContextFactory<GameContentDbContext>
    {
        public GameContentDbContext CreateDbContext() => new(options);

        public Task<GameContentDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
