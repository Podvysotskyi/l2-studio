using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Context.Identifiers;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces.Models;
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

    [Fact]
    public async Task SearchesNameKeyedSkillLookupsAndFiltersPlayerAppearancesByNumericIds()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var human = new PlayerRace { GameVersion = "c1", Id = PlayerRaceId.Human, Name = "Human" };
            var male = new PlayerSex { GameVersion = "c1", Id = PlayerSexId.Male, Name = "Male" };
            var female = new PlayerSex { GameVersion = "c1", Id = PlayerSexId.Female, Name = "Female" };
            var operateType = new SkillOperateType
            {
                GameVersion = "c1", Name = "A1", DisplayName = "Active"
            };
            var targetType = new SkillTargetType
            {
                GameVersion = "c1", Name = "ONE", DisplayName = "One"
            };
            context.AddRange(
                human,
                male,
                female,
                new PlayerFace
                {
                    GameVersion = "c1", Id = 1, Name = "Human male", PlayerRaceId = human.Id,
                    PlayerRace = human, PlayerSexId = male.Id, PlayerSex = male
                },
                new PlayerFace
                {
                    GameVersion = "c1", Id = 2, Name = "Human female", PlayerRaceId = human.Id,
                    PlayerRace = human, PlayerSexId = female.Id, PlayerSex = female
                },
                operateType,
                targetType,
                new Skill
                {
                    GameVersion = "c1", Id = 1, Levels = 1, Name = "Triple Slash",
                    SkillOperateTypeName = operateType.Name, SkillOperateType = operateType,
                    SkillTargetTypeName = targetType.Name, SkillTargetType = targetType
                });
            await context.SaveChangesAsync();
        }
        var repository = new ContentDirectoryRepository(new TestContextFactory(options));

        var races = await repository.SearchPlayerLookupsAsync(
            "c1", "player-races", new DirectoryRequest(Query: "0"), CancellationToken.None);
        var appearances = await repository.SearchPlayerAppearancesAsync(
            "c1", "player-faces", new PlayerAppearanceDirectoryRequest(PlayerRaceId: 0, PlayerSexId: 0), CancellationToken.None);
        var operateTypes = await repository.SearchSkillLookupsAsync(
            "c1", "skill-operate-types", new DirectoryRequest(), CancellationToken.None);
        var skills = await repository.SearchSkillsAsync("c1", string.Empty, 1, 25, CancellationToken.None);
        var numericSkills = await repository.SearchSkillsAsync("c1", "1", 1, 25, CancellationToken.None);

        Assert.Collection(races.Items, item => Assert.Equal(new PlayerLookupSummary(0, "Human"), item));
        Assert.Collection(appearances.Items, item => Assert.Equal("Human male", item.Name));
        Assert.Collection(operateTypes.Items, item => Assert.Equal(new SkillLookupSummary("A1", "Active"), item));
        Assert.Collection(skills.Items, item => Assert.Equal(
            new SkillSummary(1, 1, "Triple Slash", "A1", "Active", "ONE", "One", 0), item));
        Assert.Collection(numericSkills.Items, item => Assert.Equal(1, item.Id));

        var updated = await repository.UpdateSkillLookupDisplayNameAsync(
            "c1", "skill-operate-types", "A1", "Single target", CancellationToken.None);
        Assert.Equal(new SkillLookupSummary("A1", "Single target"), updated);
    }

    [Fact]
    public async Task ProjectsItemHandlersAndSkillSummaries()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var type = new ItemType { GameVersion = "c1", Name = "EtcItem", DisplayName = "Etc item" };
            var handler = new ItemHandler { GameVersion = "c1", Name = "ItemSkills", DisplayName = "Item Skills" };
            var skillType = new ItemSkillType { GameVersion = "c1", Name = "ON_CRITICAL_SKILL", DisplayName = "On Critical Skill" };
            context.AddRange(
                type,
                handler,
                skillType,
                new Skill { GameVersion = "c1", Id = 3005, Levels = 1, Name = "Bleed" },
                new Item
                {
                    GameVersion = "c1", Id = 1, Name = "Cursed Maingauche", ItemTypeName = type.Name,
                    ItemType = type, HandlerName = handler.Name, ItemHandler = handler, ItemSkill = "3005-1",
                    DisplayId = 19, Soulshots = 2, MpConsume = 5, UseCondition = "weapon",
                    IsSellable = true, UseWeaponSkillsOnly = true,
                    Skills =
                    {
                        new ItemSkill
                        {
                            GameVersion = "c1", ItemId = 1, SkillId = 3005, SkillLevel = 1,
                            ItemSkillTypeName = skillType.Name, ItemSkillType = skillType, Chance = 50
                        }
                    }
                },
                new Item
                {
                    GameVersion = "c1", Id = 2, Name = "Other", ItemTypeName = type.Name, ItemType = type
                });
            await context.SaveChangesAsync();
        }
        var repository = new ContentDirectoryRepository(new TestContextFactory(options));

        var page = await repository.SearchItemsAsync("c1", new ItemDirectoryRequest(HandlerName: "ItemSkills"), CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal("ItemSkills", item.HandlerName);
        Assert.Equal("Item Skills", item.HandlerDisplayName);
        Assert.Equal(new ItemSkillSummary(3005, 1, "Bleed", "ON_CRITICAL_SKILL", "On Critical Skill", 50), Assert.Single(item.Skills));

        var detail = await repository.GetItemAsync("c1", 1, CancellationToken.None);
        Assert.Equal(19, detail?.Properties.DisplayId);
        Assert.Equal(2, detail?.Properties.Soulshots);
        Assert.Equal(5, detail?.Properties.MpConsume);
        Assert.Equal("weapon", detail?.Properties.UseCondition);
        Assert.True(detail?.Properties.IsSellable);
        Assert.True(detail?.Properties.UseWeaponSkillsOnly);
        Assert.Equal(new ItemPrimarySkillSummary("3005-1", 3005, 1, "Bleed"), detail?.PrimarySkill);
    }

    [Fact]
    public async Task ManagesItemSkillsAndValidatesTheSelectedSkillLevel()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var type = new ItemType { GameVersion = "c1", Name = "EtcItem", DisplayName = "Etc item" };
            var skillType = new ItemSkillType { GameVersion = "c1", Name = "ON_ENCHANT_4", DisplayName = "On enchant 4" };
            context.AddRange(type, skillType,
                new Skill { GameVersion = "c1", Id = 3005, Levels = 2, Name = "Bleed" },
                new Item { GameVersion = "c1", Id = 1, Name = "Cursed Maingauche", ItemTypeName = type.Name, ItemType = type });
            await context.SaveChangesAsync();
        }
        var repository = new ContentDirectoryRepository(new TestContextFactory(options));

        var primary = await repository.SetItemPrimarySkillAsync(
            "c1", 1, new SetItemPrimarySkillRequest(3005, 2), CancellationToken.None);
        Assert.Equal(new ItemPrimarySkillSummary("3005-2", 3005, 2, "Bleed"), primary);

        var created = await repository.CreateItemSkillAsync(
            "c1", 1, new CreateItemSkillRequest(3005, 1, "ON_ENCHANT_4", 50), CancellationToken.None);
        Assert.Equal(new ItemSkillSummary(3005, 1, "Bleed", "ON_ENCHANT_4", "On enchant 4", 50), created);

        await Assert.ThrowsAsync<ItemSkillConflictException>(() => repository.CreateItemSkillAsync(
            "c1", 1, new CreateItemSkillRequest(3005, 1, null, null), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateItemSkillAsync(
            "c1", 1, new CreateItemSkillRequest(3005, 3, null, null), CancellationToken.None));

        var updated = await repository.UpdateItemSkillAsync(
            "c1", 1, 3005, 1, new UpdateItemSkillRequest(null, null), CancellationToken.None);
        Assert.Equal(new ItemSkillSummary(3005, 1, "Bleed", null, null, null), updated);
        Assert.True(await repository.DeleteItemSkillAsync("c1", 1, 3005, 1, CancellationToken.None));
        Assert.True(await repository.ClearItemPrimarySkillAsync("c1", 1, CancellationToken.None));
    }

    [Fact]
    public async Task FiltersItemsByTypeHierarchyAndProtectsParentTypes()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var weapon = new ItemType { GameVersion = "c1", Name = "Weapon", DisplayName = "Weapon" };
            var sword = new ItemType
            {
                GameVersion = "c1", Name = "SWORD", DisplayName = "Sword", ParentTypeName = weapon.Name,
                ParentType = weapon
            };
            var blunt = new ItemType
            {
                GameVersion = "c1", Name = "BLUNT", DisplayName = "Blunt", ParentTypeName = weapon.Name,
                ParentType = weapon
            };
            context.AddRange(
                weapon,
                sword,
                blunt,
                new Item { GameVersion = "c1", Id = 1, Name = "Unclassified", ItemTypeName = weapon.Name, ItemType = weapon },
                new Item { GameVersion = "c1", Id = 2, Name = "Sword", ItemTypeName = sword.Name, ItemType = sword },
                new Item { GameVersion = "c1", Id = 3, Name = "Club", ItemTypeName = blunt.Name, ItemType = blunt });
            await context.SaveChangesAsync();
        }
        var repository = new ContentDirectoryRepository(new TestContextFactory(options));

        var weapons = await repository.SearchItemsAsync("c1", new ItemDirectoryRequest(ItemTypeName: "Weapon"), CancellationToken.None);
        var swords = await repository.SearchItemsAsync("c1", new ItemDirectoryRequest(ItemTypeName: "SWORD"), CancellationToken.None);
        var types = await repository.SearchItemTypesAsync("c1", new DirectoryRequest(), CancellationToken.None);

        Assert.Equal(3, weapons.Total);
        Assert.Collection(swords.Items, item => Assert.Equal(2, item.Id));
        Assert.Contains(types.Items, type => type == new ItemTypeSummary("SWORD", "Sword", "Weapon", "Weapon"));
        await Assert.ThrowsAsync<ContentDeleteConflictException>(() =>
            repository.DeleteItemLookupAsync("c1", "item-types", "Weapon", CancellationToken.None));
    }

    private sealed class TestContextFactory(DbContextOptions<GameContentDbContext> options)
        : IDbContextFactory<GameContentDbContext>
    {
        public GameContentDbContext CreateDbContext() => new(options);

        public Task<GameContentDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
