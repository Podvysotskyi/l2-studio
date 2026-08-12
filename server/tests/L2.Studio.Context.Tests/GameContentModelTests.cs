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
