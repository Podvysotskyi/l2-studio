using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace L2.Studio.Migrations.Tests;

public sealed class GameVersionSeederTests
{
    [Fact]
    public void BuildsTheMigrationSnapshot()
    {
        var snapshotType = typeof(GameVersionSeeder).Assembly.GetType(
            "L2.Studio.Migrations.Migrations.GameContentDbContextModelSnapshot",
            throwOnError: true)!;
        var snapshot = Assert.IsAssignableFrom<ModelSnapshot>(Activator.CreateInstance(snapshotType, nonPublic: true));

        Assert.NotNull(snapshot.Model.FindEntityType(typeof(NpcStatus)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(NpcStats)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(NpcStatsVitals)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(NpcStatsAttack)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(NpcStatsDefence)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(NpcStatsSpeed)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(ItemAttackGeometry)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(ItemHandler)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(ItemSkillType)));
        Assert.NotNull(snapshot.Model.FindEntityType(typeof(ItemSkill)));
    }

    [Fact]
    public void MigrationSnapshotMatchesTheCurrentModel()
    {
        using var context = CreateNpgsqlContext();
        var snapshotType = typeof(GameVersionSeeder).Assembly.GetType(
            "L2.Studio.Migrations.Migrations.GameContentDbContextModelSnapshot",
            throwOnError: true)!;
        var snapshot = Assert.IsAssignableFrom<ModelSnapshot>(Activator.CreateInstance(snapshotType, nonPublic: true));
        var differ = context.GetService<IMigrationsModelDiffer>();
        var initializer = context.GetService<IModelRuntimeInitializer>();
        var source = initializer.Initialize(snapshot.Model, designTime: true);
        var target = context.GetService<IDesignTimeModel>().Model;

        Assert.Empty(differ.GetDifferences(source.GetRelationalModel(), target.GetRelationalModel()));
    }

    [Fact]
    public async Task SeedsAndReconcilesCanonicalVersionsWithoutRemovingCustomVersions()
    {
        await using var context = CreateContext();
        context.GameVersions.AddRange(
            new GameVersion
            {
                Key = "c1",
                DisplayName = "Old C1",
                SourceFolder = "Old",
                SortOrder = 99
            },
            new GameVersion
            {
                Key = "custom",
                DisplayName = "Custom",
                SourceFolder = "Custom",
                SortOrder = 40
            });
        await context.SaveChangesAsync();

        var seeder = new GameVersionSeeder();
        await seeder.SeedAsync(context, CancellationToken.None);
        await seeder.SeedAsync(context, CancellationToken.None);

        var versions = await context.GameVersions.OrderBy(version => version.SortOrder).ToListAsync();
        Assert.Collection(
            versions,
            version => AssertVersion(version, "c1", "Chronicle 1", "C1", 10),
            version => AssertVersion(version, "c4", "Chronicle 4", "C4", 20),
            version => AssertVersion(version, "interlude", "Interlude", "Interlude", 30),
            version => AssertVersion(version, "custom", "Custom", "Custom", 40));
    }

    [Fact]
    public void DoesNotExposeGameVersionsAsModelManagedSeedData()
    {
        using var context = CreateContext();

        var model = context.GetService<IDesignTimeModel>().Model;
        Assert.Empty(model.FindEntityType(typeof(GameVersion))!.GetSeedData());
    }

    private static GameContentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new GameContentDbContext(options);
    }

    private static GameContentDbContext CreateNpgsqlContext()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseNpgsql("Host=localhost;Database=model;Username=model;Password=model")
            .Options;
        return new GameContentDbContext(options);
    }

    private static void AssertVersion(
        GameVersion version,
        string key,
        string displayName,
        string sourceFolder,
        int sortOrder)
    {
        Assert.Equal(key, version.Key);
        Assert.Equal(displayName, version.DisplayName);
        Assert.Equal(sourceFolder, version.SourceFolder);
        Assert.Equal(sortOrder, version.SortOrder);
    }
}
