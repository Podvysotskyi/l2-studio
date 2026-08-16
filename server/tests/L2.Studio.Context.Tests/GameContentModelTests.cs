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
        Assert.Equal("import_jobs", Entity<ImportJob>(context).GetTableName());
        Assert.Equal("import_jobs", Entity<AssetImportRun>(context).GetTableName());
        Assert.Equal("import_jobs", Entity<ContentImportRun>(context).GetTableName());
        Assert.Equal("asset_catalogs", Entity<AssetCatalog>(context).GetTableName());
        Assert.Equal("skills", Entity<Skill>(context).GetTableName());
        Assert.Equal("npc_statuses", Entity<NpcStatus>(context).GetTableName());
        Assert.Equal("npc_stats", Entity<NpcStats>(context).GetTableName());
        Assert.Equal("npc_stats_vitals", Entity<NpcStatsVitals>(context).GetTableName());
        Assert.Equal("npc_stats_attack", Entity<NpcStatsAttack>(context).GetTableName());
        Assert.Equal("npc_stats_defence", Entity<NpcStatsDefence>(context).GetTableName());
        Assert.Equal("npc_stats_speed", Entity<NpcStatsSpeed>(context).GetTableName());
        Assert.Equal("item_attack_geometries", Entity<ItemAttackGeometry>(context).GetTableName());
        Assert.Equal("item_handlers", Entity<ItemHandler>(context).GetTableName());
        Assert.Equal("item_skill_types", Entity<ItemSkillType>(context).GetTableName());
        Assert.Equal("item_skills", Entity<ItemSkill>(context).GetTableName());
    }

    [Fact]
    public void PreservesScalarSchemaMetadataFromEntityAnnotations()
    {
        using var context = CreateContext();
        var item = Entity<Item>(context);

        Assert.Equal("game_version", item.FindProperty(nameof(Item.GameVersion))!.GetColumnName());
        Assert.Equal(32, item.FindProperty(nameof(Item.GameVersion))!.GetMaxLength());
        Assert.Equal("name", item.FindProperty(nameof(Item.Name))!.GetColumnName());
        Assert.Equal(100, item.FindProperty(nameof(Item.Name))!.GetMaxLength());
        Assert.Equal(ValueGenerated.Never, item.FindProperty(nameof(Item.Id))!.ValueGenerated);

        var importRun = Entity<ImportJob>(context);
        Assert.Equal("import_jobs", importRun.GetTableName());
        Assert.Equal("kind", importRun.FindProperty(nameof(ImportJob.Kind))!.GetColumnName());
        Assert.Equal(64, importRun.FindProperty(nameof(ImportJob.Kind))!.GetMaxLength());
        Assert.Equal(ValueGenerated.Never, importRun.FindProperty(nameof(ImportJob.Id))!.ValueGenerated);
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
    public void ModelsSkillLookupsByCanonicalName()
    {
        using var context = CreateContext();

        Assert.Equal(
            [nameof(SkillOperateType.GameVersion), nameof(SkillOperateType.Name)],
            Entity<SkillOperateType>(context).FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal(
            [nameof(SkillTargetType.GameVersion), nameof(SkillTargetType.Name)],
            Entity<SkillTargetType>(context).FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.False(Entity<SkillOperateType>(context).FindProperty(nameof(SkillOperateType.DisplayName))!.IsNullable);
        Assert.False(Entity<SkillTargetType>(context).FindProperty(nameof(SkillTargetType.DisplayName))!.IsNullable);

        var skill = Entity<Skill>(context);
        Assert.Contains(skill.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(SkillOperateType) &&
            key.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(Skill.GameVersion), nameof(Skill.SkillOperateTypeName)]));
        Assert.Contains(skill.GetForeignKeys(), key =>
            key.PrincipalEntityType.ClrType == typeof(SkillTargetType) &&
            key.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(Skill.GameVersion), nameof(Skill.SkillTargetTypeName)]));
    }

    [Fact]
    public void ModelsHierarchicalItemTypesAndSingleItemTypeReference()
    {
        using var context = CreateContext();
        var itemType = Entity<ItemType>(context);
        var item = Entity<Item>(context);

        Assert.Equal("parent_type_name", itemType.FindProperty(nameof(ItemType.ParentTypeName))!.GetColumnName());
        Assert.True(itemType.FindProperty(nameof(ItemType.ParentTypeName))!.IsNullable);
        Assert.Contains(itemType.GetIndexes(), index => index.Properties.Select(property => property.Name).SequenceEqual(
            [nameof(ItemType.GameVersion), nameof(ItemType.ParentTypeName)]));
        var parent = Assert.Single(itemType.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(ItemType));
        Assert.Equal(DeleteBehavior.Restrict, parent.DeleteBehavior);
        Assert.Equal([nameof(ItemType.GameVersion), nameof(ItemType.ParentTypeName)], parent.Properties.Select(property => property.Name));
        Assert.Null(item.FindProperty("WeaponType"));
        Assert.Null(item.FindProperty("ArmorType"));
        Assert.Null(item.FindProperty("EtcItemType"));
    }

    [Fact]
    public void ModelsItemAttackGeometryAsAnOptionalVersionScopedDependent()
    {
        using var context = CreateContext();
        var geometry = Entity<ItemAttackGeometry>(context);

        Assert.Equal(
            [nameof(ItemAttackGeometry.GameVersion), nameof(ItemAttackGeometry.ItemId)],
            geometry.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal("offset_x", geometry.FindProperty(nameof(ItemAttackGeometry.OffsetX))!.GetColumnName());
        Assert.Equal("offset_y", geometry.FindProperty(nameof(ItemAttackGeometry.OffsetY))!.GetColumnName());
        Assert.Equal("radius", geometry.FindProperty(nameof(ItemAttackGeometry.Radius))!.GetColumnName());
        Assert.Equal("length", geometry.FindProperty(nameof(ItemAttackGeometry.Length))!.GetColumnName());
        var item = Assert.Single(geometry.GetForeignKeys());
        Assert.Equal(typeof(Item), item.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, item.DeleteBehavior);
        Assert.Equal(
            [nameof(ItemAttackGeometry.GameVersion), nameof(ItemAttackGeometry.ItemId)],
            item.Properties.Select(property => property.Name));
    }

    [Fact]
    public void ModelsVersionScopedItemHandlersAndSkills()
    {
        using var context = CreateContext();

        var handler = Entity<ItemHandler>(context);
        var skillType = Entity<ItemSkillType>(context);
        Assert.Equal([nameof(ItemHandler.GameVersion), nameof(ItemHandler.Name)], handler.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal([nameof(ItemSkillType.GameVersion), nameof(ItemSkillType.Name)], skillType.FindPrimaryKey()!.Properties.Select(property => property.Name));

        var item = Entity<Item>(context);
        Assert.Contains(item.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(ItemHandler) &&
            key.DeleteBehavior == DeleteBehavior.Restrict && key.Properties.Select(property => property.Name).SequenceEqual(
                [nameof(Item.GameVersion), nameof(Item.HandlerName)]));

        var skill = Entity<ItemSkill>(context);
        Assert.Equal(
            [nameof(ItemSkill.GameVersion), nameof(ItemSkill.ItemId), nameof(ItemSkill.SkillId), nameof(ItemSkill.SkillLevel)],
            skill.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal("item_skill_type_name", skill.FindProperty(nameof(ItemSkill.ItemSkillTypeName))!.GetColumnName());
        Assert.True(skill.FindProperty(nameof(ItemSkill.ItemSkillTypeName))!.IsNullable);
        Assert.Equal("chance", skill.FindProperty(nameof(ItemSkill.Chance))!.GetColumnName());
        Assert.True(skill.FindProperty(nameof(ItemSkill.Chance))!.IsNullable);
        Assert.Contains(skill.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(Item) && key.DeleteBehavior == DeleteBehavior.Cascade);
        Assert.Contains(skill.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(ItemSkillType) && key.DeleteBehavior == DeleteBehavior.Restrict);
        Assert.DoesNotContain(skill.GetForeignKeys(), key => key.PrincipalEntityType.ClrType == typeof(Skill));
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

        var importRun = Entity<ContentImportRun>(context);
        Assert.Equal("add_missing", importRun.FindProperty(nameof(ContentImportRun.Mode))!.GetDefaultValue());
        Assert.Equal("restored_count", importRun.FindProperty(nameof(ContentImportRun.RestoredCount))!.GetColumnName());
        Assert.Contains(importRun.GetIndexes(), index => index.IsUnique &&
            index.GetFilter() == "category = 'content' AND status IN ('queued', 'running')");
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
