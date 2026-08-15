using L2.Studio.Context;
using L2.Studio.Context.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace L2.Studio.Context.Tests;

public sealed class GameContentModelTests
{
    [Fact]
    public void UsesTheContentSchemaAndExpectedTableNames()
    {
        using var context = CreateContext();

        Assert.Equal(GameContentDbContext.SchemaName, context.Model.GetDefaultSchema());
        Assert.Equal("player_classes", Entity<PlayerClass>(context).GetTableName());
        Assert.Equal("asset_import_runs", Entity<AssetImportRun>(context).GetTableName());
        Assert.Equal("asset_catalogs", Entity<AssetCatalog>(context).GetTableName());
        Assert.Equal("skills", Entity<Skill>(context).GetTableName());
        Assert.Equal("npc_statuses", Entity<NpcStatus>(context).GetTableName());
        Assert.Equal("npc_stats", Entity<NpcStats>(context).GetTableName());
        Assert.Equal("npc_stats_vitals", Entity<NpcStatsVitals>(context).GetTableName());
        Assert.Equal("npc_stats_attack", Entity<NpcStatsAttack>(context).GetTableName());
        Assert.Equal("npc_stats_defence", Entity<NpcStatsDefence>(context).GetTableName());
        Assert.Equal("npc_stats_speed", Entity<NpcStatsSpeed>(context).GetTableName());
    }

    [Fact]
    public void ModelsPlayerClassesPerRaceAndSex()
    {
        using var context = CreateContext();
        var entity = Entity<PlayerClass>(context);

        Assert.Equal(
            [nameof(PlayerClass.GameVersion), nameof(PlayerClass.Id), nameof(PlayerClass.PlayerSexId), nameof(PlayerClass.PlayerRaceId)],
            entity.FindPrimaryKey()!.Properties.Select(property => property.Name));
        var parent = Assert.Single(entity.GetForeignKeys(), foreignKey =>
            foreignKey.PrincipalEntityType.ClrType == typeof(PlayerClass));
        Assert.Equal(DeleteBehavior.Restrict, parent.DeleteBehavior);
        Assert.Equal(
            [nameof(PlayerClass.GameVersion), nameof(PlayerClass.ParentClassId), nameof(PlayerClass.PlayerSexId), nameof(PlayerClass.PlayerRaceId)],
            parent.Properties.Select(property => property.Name));
    }

    [Fact]
    public void ModelsNpcLookupsByCanonicalNameAndLimitsActiveImports()
    {
        using var context = CreateContext();

        Assert.Equal(
            [nameof(NpcType.GameVersion), nameof(NpcType.Name)],
            Entity<NpcType>(context).FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(
            [nameof(NpcRace.GameVersion), nameof(NpcRace.Name)],
            Entity<NpcRace>(context).FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(
            [nameof(NpcSex.GameVersion), nameof(NpcSex.Name)],
            Entity<NpcSex>(context).FindPrimaryKey()!.Properties.Select(property => property.Name));

        var npc = Entity<Npc>(context);
        Assert.Contains(npc.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(NpcType) &&
            key.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(Npc.GameVersion), nameof(Npc.NpcTypeName)]));
        Assert.Contains(npc.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(NpcRace) &&
            key.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(Npc.GameVersion), nameof(Npc.NpcRaceName)]));
        Assert.Contains(npc.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(NpcSex) &&
            key.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(Npc.GameVersion), nameof(Npc.NpcSexName)]));

        var status = Entity<NpcStatus>(context);
        Assert.Equal(
            [nameof(NpcStatus.GameVersion), nameof(NpcStatus.NpcId)],
            status.FindPrimaryKey()!.Properties.Select(property => property.Name));
        var statusNpc = Assert.Single(status.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(Npc));
        Assert.Equal(DeleteBehavior.Cascade, statusNpc.DeleteBehavior);
        Assert.Equal(
            [nameof(NpcStatus.GameVersion), nameof(NpcStatus.NpcId)],
            statusNpc.Properties.Select(property => property.Name));
        Assert.All(
            new[]
            {
                nameof(NpcStatus.Attackable), nameof(NpcStatus.Targetable), nameof(NpcStatus.Talkable),
                nameof(NpcStatus.Undying), nameof(NpcStatus.ShowName), nameof(NpcStatus.RandomWalk),
                nameof(NpcStatus.CanMove), nameof(NpcStatus.NoSleepMode), nameof(NpcStatus.CanBeSown)
            },
            property => Assert.False(status.FindProperty(property)!.IsNullable));

        Assert.All(
            new IEntityType[]
            {
                Entity<NpcStats>(context), Entity<NpcStatsVitals>(context), Entity<NpcStatsAttack>(context),
                Entity<NpcStatsDefence>(context), Entity<NpcStatsSpeed>(context)
            },
            stats =>
            {
                Assert.Equal(["GameVersion", "NpcId"], stats.FindPrimaryKey()!.Properties.Select(property => property.Name));
                Assert.Contains(stats.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(Npc) && key.DeleteBehavior == DeleteBehavior.Cascade);
            });
        var npcStats = Entity<NpcStats>(context);
        Assert.True(npcStats.FindProperty(nameof(NpcStats.HitTime))!.IsNullable);
        Assert.Equal("hit_time", npcStats.FindProperty(nameof(NpcStats.HitTime))!.GetColumnName());

        var importRun = Entity<NpcLookupImportRun>(context);
        Assert.Equal("add_missing", importRun.FindProperty(nameof(NpcLookupImportRun.Mode))!.GetDefaultValue());
        Assert.Equal("restored_count", importRun.FindProperty(nameof(NpcLookupImportRun.RestoredCount))!.GetColumnName());
        Assert.Contains(importRun.GetIndexes(), index =>
            index.IsUnique && index.GetFilter() == "status IN ('queued', 'running')");
    }

    [Fact]
    public void ModelsImportOwnershipWithCascadingDeletes()
    {
        using var context = CreateContext();

        var workItemRun = Assert.Single(Entity<AssetImportWorkItem>(context).GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(AssetImportRun));
        var diagnosticRun = Assert.Single(Entity<AssetImportDiagnostic>(context).GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(AssetImportRun));
        var catalogSource = Assert.Single(Entity<AssetCatalogSource>(context).GetForeignKeys(),
            key => key.PrincipalEntityType.ClrType == typeof(AssetCatalog));

        Assert.Equal(DeleteBehavior.Cascade, workItemRun.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, diagnosticRun.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, catalogSource.DeleteBehavior);
        Assert.Contains(Entity<AssetCatalogGroup>(context).GetIndexes(), index =>
            !index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(AssetCatalogGroup.CatalogId), nameof(AssetCatalogGroup.Name)]));
    }

    [Fact]
    public void MapsJsonDocumentsAndIgnoresCompatibilityAliases()
    {
        using var context = CreateContext();

        Assert.Equal(
            "jsonb",
            Entity<AssetCatalog>(context)
                .FindProperty(nameof(AssetCatalog.MetadataJson))!
                .GetColumnType());
        var workItem = Entity<AssetImportWorkItem>(context);
        Assert.Null(workItem.FindProperty(nameof(AssetImportWorkItem.Kind)));
        Assert.Null(workItem.FindProperty(nameof(AssetImportWorkItem.TotalCount)));
        Assert.Null(workItem.FindProperty(nameof(AssetImportWorkItem.ProcessedCount)));
        Assert.Null(workItem.FindProperty(nameof(AssetImportWorkItem.SkippedCount)));
        Assert.Null(workItem.FindProperty(nameof(AssetImportWorkItem.WarningsJson)));
    }

    [Fact]
    public void ModelsImmutableArtifactInventoryAndPublicationPointers()
    {
        using var context = CreateContext();

        var artifact = Entity<AssetArtifact>(context);
        var file = Entity<AssetArtifactFile>(context);
        var dependency = Entity<AssetArtifactDependency>(context);
        var publication = Entity<AssetCatalogSource>(context);

        Assert.Equal("asset_artifacts", artifact.GetTableName());
        Assert.Contains(artifact.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(AssetArtifact.GameVersion), nameof(AssetArtifact.Kind),
                nameof(AssetArtifact.NormalizedSourceKey), nameof(AssetArtifact.BuildFingerprint)
            ]));
        Assert.Contains(file.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(AssetArtifactFile.ArtifactId), nameof(AssetArtifactFile.RelativePath)
            ]));
        Assert.Contains(dependency.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(AssetArtifact));
        Assert.Contains(publication.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(AssetArtifact) &&
            key.DeleteBehavior == DeleteBehavior.Restrict);
    }

    [Fact]
    public void ModelsImmutableReleaseSnapshotsAndLivePointers()
    {
        using var context = CreateContext();

        var release = Entity<AssetRelease>(context);
        var artifact = Entity<AssetReleaseArtifact>(context);
        var pointer = Entity<AssetReleasePointer>(context);

        Assert.Equal("asset_releases", release.GetTableName());
        Assert.Contains(release.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(AssetRelease.GameVersion), nameof(AssetRelease.Name)
            ]));
        Assert.Equal([nameof(AssetReleaseArtifact.ReleaseId), nameof(AssetReleaseArtifact.ArtifactId)],
            artifact.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Contains(artifact.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(AssetArtifact) &&
            key.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.Equal(nameof(AssetReleasePointer.GameVersion), Assert.Single(pointer.FindPrimaryKey()!.Properties).Name);
        Assert.Equal(2, pointer.GetForeignKeys().Count(key => key.PrincipalEntityType.ClrType == typeof(AssetRelease)));
    }

    private static GameContentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql("Host=localhost;Database=model;Username=model;Password=model")
            .Options;
        return new GameContentDbContext(options);
    }

    private static IEntityType Entity<TEntity>(GameContentDbContext context) =>
        context.Model.FindEntityType(typeof(TEntity))!;
}
