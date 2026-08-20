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
    public async Task ResolvesGroupedIconPackageTexturesUsingItemBodyPartContext()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var catalog = new AssetCatalog
            {
                Id = Guid.NewGuid(), GameVersion = "c1", Kind = "textures",
                SourceFolder = "SysTextures", SourceHash = "catalog-hash", SchemaVersion = 9,
                MetadataJson = "{}", IsActive = true, PublishedAt = DateTimeOffset.UnixEpoch
            };
            var source = new AssetCatalogSource
            {
                Id = Guid.NewGuid(), Catalog = catalog, ArtifactId = Guid.NewGuid(), PublishingWorkItemId = Guid.NewGuid(),
                SourceKey = "SysTextures/Icon.utx", NormalizedSourceKey = "systextures/icon.utx",
                SourceHash = "source-hash", OutputRoot = "versions/c1/textures/icon", MetadataJson = "{}",
                ReferencedOutputRootsJson = "[]", PublishedAt = DateTimeOffset.UnixEpoch
            };
            context.AddRange(
                new AssetCatalogItem
                {
                    Catalog = catalog, Source = source, Name = "weapon_i.weapon_sword_i00", GroupName = "Icon",
                    Status = "resolved", MetadataJson = "{\"url\":\"/versions/c1/textures/icon/sword.webp\"}"
                },
                new AssetCatalogItem
                {
                    Catalog = catalog, Source = source, Name = "upbody_i.armor_hard_leather_shirt_i00", GroupName = "Icon",
                    Status = "resolved", MetadataJson = "{\"url\":\"/versions/c1/textures/icon/chest.webp\"}"
                },
                new AssetCatalogItem
                {
                    Catalog = catalog, Source = source, Name = "lowbody_i.armor_hard_leather_shirt_i00", GroupName = "Icon",
                    Status = "resolved", MetadataJson = "{\"url\":\"/versions/c1/textures/icon/legs.webp\"}"
                },
                new AssetCatalogItem
                {
                    Catalog = catalog, Source = source, Name = "weapon_i.weapon_other_i00", GroupName = "Other",
                    Status = "resolved", MetadataJson = "{\"url\":\"/versions/c1/textures/other.webp\"}"
                },
                new AssetCatalogItem
                {
                    Catalog = catalog, Source = source, Name = "weapon_i.weapon_skipped_i00", GroupName = "Icon",
                    Status = "skipped", MetadataJson = "{\"url\":\"/versions/c1/textures/skipped.webp\"}"
                });
            await context.SaveChangesAsync();
        }
        var repository = new ContentDirectoryRepository(new TestContextFactory(options));

        var icons = await repository.ResolveItemIconsAsync(
            "c1",
            [
                new ItemIconReference(1, "icon.weapon_sword_i00", null),
                new ItemIconReference(2, "icon.armor_hard_leather_shirt_i00", "chest"),
                new ItemIconReference(3, "icon.armor_hard_leather_shirt_i00", "legs"),
                new ItemIconReference(4, "icon.weapon_other_i00", null),
                new ItemIconReference(5, "icon.weapon_skipped_i00", null)
            ],
            CancellationToken.None);

        Assert.Equal([
            new ItemIconSummary(1, "/versions/c1/textures/icon/sword.webp"),
            new ItemIconSummary(2, "/versions/c1/textures/icon/chest.webp"),
            new ItemIconSummary(3, "/versions/c1/textures/icon/legs.webp")
        ], icons);
    }

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
        var skill = await repository.GetSkillAsync("c1", 1, CancellationToken.None);
        var missingSkill = await repository.GetSkillAsync("c1", 2, CancellationToken.None);

        Assert.Collection(races.Items, item => Assert.Equal(new PlayerLookupSummary(0, "Human"), item));
        Assert.Collection(appearances.Items, item => Assert.Equal("Human male", item.Name));
        Assert.Collection(operateTypes.Items, item => Assert.Equal(new SkillLookupSummary("A1", "Active"), item));
        Assert.Collection(skills.Items, item => Assert.Equal(
            new SkillSummary(1, 1, "Triple Slash", "A1", "Active", "ONE", "One", 0), item));
        Assert.Collection(numericSkills.Items, item => Assert.Equal(1, item.Id));
        Assert.Equal(new SkillSummary(1, 1, "Triple Slash", "A1", "Active", "ONE", "One", 0), skill);
        Assert.Null(missingSkill);

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
                    ItemType = type,
                    Etc = new Item_Etc { GameVersion = "c1", ItemId = 1, HandlerName = handler.Name, ItemHandler = handler, ItemSkill = "3005-1", DisplayId = 19, UseCondition = "weapon" },
                    BehaviorAvailability = new ItemBehaviorAvailability { GameVersion = "c1", ItemId = 1, IsSellable = true, IsStackable = false },
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
                    GameVersion = "c1", Id = 2, Name = "Other", ItemTypeName = type.Name, ItemType = type,
                    Etc = new Item_Etc { GameVersion = "c1", ItemId = 2 }
                });
            await context.SaveChangesAsync();
        }
        var repository = new ItemRepository(new TestContextFactory(options));

        var page = await repository.SearchItemsAsync("c1", ItemFamilyValues.Etc, new ItemDirectoryRequest(HandlerName: "ItemSkills"), CancellationToken.None);

        var item = Assert.Single(page.Items);
        Assert.Equal("ItemSkills", item.HandlerName);
        Assert.Equal("Item Skills", item.HandlerDisplayName);
        Assert.Equal(new ItemSkillSummary(3005, 1, "Bleed", "ON_CRITICAL_SKILL", "On Critical Skill", 50), Assert.Single(item.Skills));

        var detail = await repository.GetItemAsync("c1", ItemFamilyValues.Etc, 1, CancellationToken.None);
        Assert.Equal(19, detail?.Properties.DisplayId);
        Assert.Equal("weapon", detail?.Properties.UseCondition);
        Assert.True(detail?.BehaviorAvailability?.IsSellable);
        Assert.False(detail?.BehaviorAvailability?.IsStackable);
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
                new Item { GameVersion = "c1", Id = 1, Name = "Cursed Maingauche", ItemTypeName = type.Name, ItemType = type, Etc = new Item_Etc { GameVersion = "c1", ItemId = 1 } });
            await context.SaveChangesAsync();
        }
        var repository = new ItemRepository(new TestContextFactory(options));

        var primary = await repository.SetItemPrimarySkillAsync(
            "c1", ItemFamilyValues.Etc, 1, new SetItemPrimarySkillRequest(3005, 2), CancellationToken.None);
        Assert.Equal(new ItemPrimarySkillSummary("3005-2", 3005, 2, "Bleed"), primary);

        var created = await repository.CreateItemSkillAsync(
            "c1", ItemFamilyValues.Etc, 1, new CreateItemSkillRequest(3005, 1, "ON_ENCHANT_4", 50), CancellationToken.None);
        Assert.Equal(new ItemSkillSummary(3005, 1, "Bleed", "ON_ENCHANT_4", "On enchant 4", 50), created);

        await Assert.ThrowsAsync<ItemSkillConflictException>(() => repository.CreateItemSkillAsync(
            "c1", ItemFamilyValues.Etc, 1, new CreateItemSkillRequest(3005, 1, null, null), CancellationToken.None));
        await Assert.ThrowsAsync<InvalidOperationException>(() => repository.CreateItemSkillAsync(
            "c1", ItemFamilyValues.Etc, 1, new CreateItemSkillRequest(3005, 3, null, null), CancellationToken.None));

        var updated = await repository.UpdateItemSkillAsync(
            "c1", ItemFamilyValues.Etc, 1, 3005, 1, new UpdateItemSkillRequest(null, null), CancellationToken.None);
        Assert.Equal(new ItemSkillSummary(3005, 1, "Bleed", null, null, null), updated);
        Assert.True(await repository.DeleteItemSkillAsync("c1", ItemFamilyValues.Etc, 1, 3005, 1, CancellationToken.None));
        Assert.True(await repository.ClearItemPrimarySkillAsync("c1", ItemFamilyValues.Etc, 1, CancellationToken.None));
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
            var armor = new ItemType { GameVersion = "c1", Name = "Armor", DisplayName = "Armor" };
            var heavy = new ItemType
            {
                GameVersion = "c1", Name = "HEAVY", DisplayName = "Heavy", ParentTypeName = armor.Name,
                ParentType = armor
            };
            var etcItem = new ItemType { GameVersion = "c1", Name = "EtcItem", DisplayName = "Etc item" };
            context.AddRange(
                weapon,
                sword,
                blunt,
                armor,
                heavy,
                etcItem,
                new Item { GameVersion = "c1", Id = 1, Name = "Unclassified", ItemTypeName = weapon.Name, ItemType = weapon, Weapon = new Item_Weapon { GameVersion = "c1", ItemId = 1 } },
                new Item { GameVersion = "c1", Id = 2, Name = "Sword", ItemTypeName = sword.Name, ItemType = sword, Weapon = new Item_Weapon { GameVersion = "c1", ItemId = 2 } },
                new Item { GameVersion = "c1", Id = 3, Name = "Club", ItemTypeName = blunt.Name, ItemType = blunt, Weapon = new Item_Weapon { GameVersion = "c1", ItemId = 3 } },
                new Item { GameVersion = "c1", Id = 4, Name = "Tunic", ItemTypeName = heavy.Name, ItemType = heavy, Armor = new Item_Armor { GameVersion = "c1", ItemId = 4 } },
                new Item { GameVersion = "c1", Id = 5, Name = "Potion", ItemTypeName = etcItem.Name, ItemType = etcItem, Etc = new Item_Etc { GameVersion = "c1", ItemId = 5 } });
            await context.SaveChangesAsync();
        }
        var repository = new ItemRepository(new TestContextFactory(options));

        var weapons = await repository.SearchItemsAsync("c1", ItemFamilyValues.Weapon, new ItemDirectoryRequest(ItemTypeName: "Weapon"), CancellationToken.None);
        var swords = await repository.SearchItemsAsync("c1", ItemFamilyValues.Weapon, new ItemDirectoryRequest(ItemTypeName: "SWORD"), CancellationToken.None);
        var armorItems = await repository.SearchItemsAsync("c1", ItemFamilyValues.Armor, new ItemDirectoryRequest(), CancellationToken.None);
        var otherItems = await repository.SearchItemsAsync("c1", ItemFamilyValues.Etc, new ItemDirectoryRequest(), CancellationToken.None);
        var weaponSwords = await repository.SearchItemsAsync("c1", ItemFamilyValues.Weapon, new ItemDirectoryRequest(ItemTypeName: "SWORD"), CancellationToken.None);
        var types = await repository.SearchItemTypesAsync("c1", new DirectoryRequest(), CancellationToken.None);

        Assert.Equal(3, weapons.Total);
        Assert.Collection(swords.Items, item => Assert.Equal(2, item.Id));
        Assert.Collection(armorItems.Items, item => Assert.Equal(4, item.Id));
        Assert.Collection(otherItems.Items, item => Assert.Equal(5, item.Id));
        Assert.Collection(weaponSwords.Items, item => Assert.Equal(2, item.Id));
        Assert.Contains(types.Items, type => type == new ItemTypeSummary("SWORD", "Sword", "Weapon", "Weapon"));
        await Assert.ThrowsAsync<ContentDeleteConflictException>(() =>
            repository.DeleteItemLookupAsync("c1", "item-types", "Weapon", CancellationToken.None));
    }

    [Fact]
    public async Task ProjectsVersionScopedCraftingRecipesAndRecipeTypes()
    {
        var options = new DbContextOptionsBuilder<GameContentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using (var context = new GameContentDbContext(options))
        {
            var c1ItemType = new ItemType { GameVersion = "c1", Name = "EtcItem", DisplayName = "Etc item" };
            var c4ItemType = new ItemType { GameVersion = "c4", Name = "EtcItem", DisplayName = "Etc item" };
            var dwarven = new ItemRecipeType { GameVersion = "c1", Name = "dwarven" };
            var common = new ItemRecipeType { GameVersion = "c1", Name = "common" };
            var c4Type = new ItemRecipeType { GameVersion = "c4", Name = "dwarven" };
            var craftingRecipe = new ItemRecipe
            {
                GameVersion = "c1", Id = 1, Name = "Craft Mithril Dagger", ItemRecipeTypeName = dwarven.Name,
                ItemRecipeType = dwarven, CraftLevel = 3, SuccessRate = 60,
                StatUse = new ItemRecipeStatUse { GameVersion = "c1", ItemRecipeId = 1, Mp = 24 }
            };
            craftingRecipe.Ingredients.Add(new ItemRecipeIngredient { GameVersion = "c1", ItemRecipeId = 1, ItemId = 57, Count = 500 });
            craftingRecipe.Productions.Add(new ItemRecipeProduction { GameVersion = "c1", ItemRecipeId = 1, ItemId = 222, Count = 1 });
            context.AddRange(
                c1ItemType, c4ItemType, dwarven, common, c4Type,
                new Item { GameVersion = "c1", Id = 57, Name = "Adena", ItemTypeName = c1ItemType.Name, ItemType = c1ItemType },
                new Item { GameVersion = "c1", Id = 222, Name = "Mithril Dagger", ItemTypeName = c1ItemType.Name, ItemType = c1ItemType },
                craftingRecipe,
                new ItemRecipe
                {
                    GameVersion = "c4", Id = 1, Name = "Other version", ItemRecipeTypeName = c4Type.Name,
                    ItemRecipeType = c4Type, CraftLevel = 1, SuccessRate = 100
                });
            await context.SaveChangesAsync();
        }
        var repository = new ItemRepository(new TestContextFactory(options));

        var recipes = await repository.SearchItemRecipesAsync("c1", new DirectoryRequest(), CancellationToken.None);
        var types = await repository.SearchItemRecipeTypesAsync("c1", new DirectoryRequest(), CancellationToken.None);

        Assert.Equal(1, recipes.Total);
        var recipe = Assert.Single(recipes.Items);
        Assert.Equal(new ItemRecipeStatUseSummary(24, null), recipe.StatUse);
        Assert.Equal(new ItemRecipeItemSummary(57, "Adena", 500), Assert.Single(recipe.Ingredients));
        Assert.Equal(new ItemRecipeItemSummary(222, "Mithril Dagger", 1), Assert.Single(recipe.Productions));
        Assert.Equal(
            [new ItemRecipeTypeSummary("common", 0), new ItemRecipeTypeSummary("dwarven", 1)],
            types.Items);
    }

    private sealed class TestContextFactory(DbContextOptions<GameContentDbContext> options)
        : IDbContextFactory<GameContentDbContext>
    {
        public GameContentDbContext CreateDbContext() => new(options);

        public Task<GameContentDbContext> CreateDbContextAsync(
            CancellationToken cancellationToken = default) => Task.FromResult(CreateDbContext());
    }
}
