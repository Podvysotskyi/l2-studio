using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace L2.Studio.Repositories.Tests;

public sealed class ContentDirectoryRepositoryTests
{
    [Fact]
    public async Task MarksOnlyNpcsInTheActiveAppearanceManifestIndexAsVisual()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var type = new NpcType { GameVersion = "c1", Name = "Monster", DisplayName = "Monster" };
            var sex = new NpcSex { GameVersion = "c1", Name = "MALE", DisplayName = "Male" };
            context.AddRange(
                type,
                sex,
                new Npc
                {
                    GameVersion = "c1", Id = 1, AppearanceId = 101, Level = 10, Name = "Visible",
                    NpcTypeName = type.Name, NpcType = type, NpcSexName = sex.Name, NpcSex = sex,
                    Status = new NpcStatus
                    {
                        GameVersion = "c1", NpcId = 1, Attackable = true, Targetable = false,
                        Talkable = true, Undying = false, ShowName = true, RandomWalk = false,
                        CanMove = true, NoSleepMode = false, CanBeSown = true
                    }
                },
                new Npc
                {
                    GameVersion = "c1", Id = 2, AppearanceId = 102, Level = 10, Name = "Hidden",
                    NpcTypeName = type.Name, NpcType = type, NpcSexName = sex.Name, NpcSex = sex
                },
                new AssetCatalog
                {
                    Id = Guid.NewGuid(), GameVersion = "c1", Kind = "npcappearances",
                    SourceFolder = "system", SourceHash = "catalog-hash",
                    SchemaVersion = 6,
                    MetadataJson = "{\"npcIds\":[1]}", IsActive = true,
                    PublishedAt = DateTimeOffset.UnixEpoch
                });
            await context.SaveChangesAsync();
        }
        var repository = new ContentDirectoryRepository(new TestContextFactory(options));

        var visible = await repository.GetNpcAsync("c1", 1, CancellationToken.None);
        var hidden = await repository.GetNpcAsync("c1", 2, CancellationToken.None);

        Assert.True(visible is { HasVisuals: true });
        Assert.True(hidden is { HasVisuals: false });
        Assert.Equal(new NpcStatusSummary(true, false, true, false, true, false, true, false, true), visible?.Status);
        Assert.Null(hidden?.Status);
    }

    [Fact]
    public async Task DoesNotTreatLegacyAppearanceIndexesAsNpcVisuals()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var type = new NpcType { GameVersion = "c1", Name = "Monster", DisplayName = "Monster" };
            var sex = new NpcSex { GameVersion = "c1", Name = "MALE", DisplayName = "Male" };
            context.AddRange(
                type,
                sex,
                new Npc
                {
                    GameVersion = "c1", Id = 501, AppearanceId = 7217, Level = 85, Name = "Sentinel",
                    NpcTypeName = type.Name, NpcType = type, NpcSexName = sex.Name, NpcSex = sex
                },
                new AssetCatalog
                {
                    Id = Guid.NewGuid(), GameVersion = "c1", Kind = "npcappearances",
                    SourceFolder = "system", SourceHash = "legacy-hash", SchemaVersion = 5,
                    MetadataJson = "{\"npcIds\":[501]}", IsActive = true,
                    PublishedAt = DateTimeOffset.UnixEpoch
                });
            await context.SaveChangesAsync();
        }
        var repository = new ContentDirectoryRepository(new TestContextFactory(options));

        var npc = await repository.GetNpcAsync("c1", 501, CancellationToken.None);

        Assert.True(npc is { HasVisuals: false });
    }

    [Fact]
    public async Task FiltersNpcsBeforeCountingAndPaginating()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var monster = new NpcType { GameVersion = "c1", Name = "Monster", DisplayName = "Monster" };
            var guard = new NpcType { GameVersion = "c1", Name = "Guard", DisplayName = "Guard" };
            var humanoid = new NpcRace { GameVersion = "c1", Name = "HUMANOID", DisplayName = "Humanoid" };
            var male = new NpcSex { GameVersion = "c1", Name = "MALE", DisplayName = "Male" };
            var female = new NpcSex { GameVersion = "c1", Name = "FEMALE", DisplayName = "Female" };
            context.AddRange(
                monster,
                guard,
                humanoid,
                male,
                female,
                new Npc
                {
                    GameVersion = "c1", Id = 1, Level = 10, Name = "Visible Monster",
                    NpcTypeName = monster.Name, NpcType = monster,
                    NpcRaceName = humanoid.Name, NpcRace = humanoid,
                    NpcSexName = male.Name, NpcSex = male
                },
                new Npc
                {
                    GameVersion = "c1", Id = 2, Level = 15, Name = "Unraced Monster",
                    NpcTypeName = monster.Name, NpcType = monster,
                    NpcSexName = male.Name, NpcSex = male
                },
                new Npc
                {
                    GameVersion = "c1", Id = 3, Level = 20, Name = "Visible Guard",
                    NpcTypeName = guard.Name, NpcType = guard,
                    NpcRaceName = humanoid.Name, NpcRace = humanoid,
                    NpcSexName = female.Name, NpcSex = female
                },
                new AssetCatalog
                {
                    Id = Guid.NewGuid(), GameVersion = "c1", Kind = "npcappearances",
                    SourceFolder = "system", SourceHash = "catalog-hash", SchemaVersion = 6,
                    MetadataJson = "{\"npcIds\":[1,3]}", IsActive = true,
                    PublishedAt = DateTimeOffset.UnixEpoch
                });
            await context.SaveChangesAsync();
        }
        var repository = new ContentDirectoryRepository(new TestContextFactory(options));

        var visibleMonsters = await repository.SearchNpcsAsync(
            "c1", new NpcDirectoryRequest(NpcTypeName: "Monster", NpcRaceName: "HUMANOID", NpcSexName: "MALE", HasVisuals: true), CancellationToken.None);
        var unraced = await repository.SearchNpcsAsync(
            "c1", new NpcDirectoryRequest(WithoutRace: true), CancellationToken.None);
        var withoutVisuals = await repository.SearchNpcsAsync(
            "c1", new NpcDirectoryRequest(HasVisuals: false), CancellationToken.None);

        Assert.Equal(1, visibleMonsters.Total);
        Assert.Collection(visibleMonsters.Items, npc => Assert.Equal(1, npc.Id));
        Assert.Equal(1, unraced.Total);
        Assert.Collection(unraced.Items, npc => Assert.Equal(2, npc.Id));
        Assert.Equal(1, withoutVisuals.Total);
        Assert.Collection(withoutVisuals.Items, npc => Assert.Equal(2, npc.Id));
    }

    private sealed class TestContextFactory(DbContextOptions<GameContentDbContext> options)
        : IDbContextFactory<GameContentDbContext>
    {
        public GameContentDbContext CreateDbContext() => new(options);

        public Task<GameContentDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
