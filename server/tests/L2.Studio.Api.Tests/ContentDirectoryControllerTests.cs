using L2.Studio.Api.Controllers;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Xunit;

namespace L2.Studio.Api.Tests;

public sealed class ContentDirectoryControllerTests
{
    [Fact]
    public async Task SearchesNpcsWithANormalizedQuery()
    {
        var expected = new NpcDirectoryPage([], 4, 2, 50);
        var repository = new StubContentDirectoryRepository { Npcs = expected };
        var controller = new ContentDirectoryController(repository);
        using var cancellation = new CancellationTokenSource();

        var result = await controller.SearchNpcs(
            "interlude",
            new NpcDirectoryRequest("  Goblin  ", 2, 50, " Monster ", " HUMANOID ", null, " MALE ", true),
            cancellation.Token);

        Assert.Same(expected, result);
        Assert.Equal("Goblin", repository.NpcQuery);
        Assert.Equal(2, repository.NpcPage);
        Assert.Equal(50, repository.NpcPageSize);
        Assert.Equal(cancellation.Token, repository.NpcToken);
        Assert.Equal("Monster", repository.NpcRequest?.NpcTypeName);
        Assert.Equal("HUMANOID", repository.NpcRequest?.NpcRaceName);
        Assert.Equal("MALE", repository.NpcRequest?.NpcSexName);
        Assert.True(repository.NpcRequest?.HasVisuals);
    }

    [Fact]
    public async Task SearchesSkillsWithAnEmptyQueryWhenItIsMissing()
    {
        var repository = new StubContentDirectoryRepository();
        var controller = new ContentDirectoryController(repository);

        await controller.SearchSkills("interlude", new DirectoryRequest(null, 1, 25), CancellationToken.None);

        Assert.Equal(string.Empty, repository.SkillQuery);
    }

    [Fact]
    public async Task GetsSkillDefinitionsOrReturnsNotFound()
    {
        var expected = new SkillSummary(3006, 1, "Armor Set", "A2", "Active", "SELF", "Self", 1);
        var repository = new StubContentDirectoryRepository { Skill = expected };
        var controller = new ContentDirectoryController(repository);

        var result = await controller.GetSkill("c1", 3006, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, response.Value);
        Assert.Equal(3006, repository.SkillId);
        var missing = await new ContentDirectoryController(new StubContentDirectoryRepository())
            .GetSkill("c1", 9999, CancellationToken.None);
        Assert.IsType<NotFoundResult>(missing.Result);
    }

    [Fact]
    public async Task DelegatesItemSearchToTheRequestedFamily()
    {
        var repository = new StubContentDirectoryRepository();
        var controller = new ContentDirectoryController(repository);

        await controller.SearchItems(
            "c1",
            "weapon",
            new ItemDirectoryRequest(Query: "  blade  "),
            CancellationToken.None);

        Assert.Equal("weapon", repository.ItemFamily);
        Assert.Equal("blade", repository.ItemRequest?.Query);
    }

    [Fact]
    public async Task DelegatesLookupRequestsToTheMatchingRepositoryMethod()
    {
        var expected = new DirectoryPage<NpcLookupSummary>([new("Humanoid", "Humanoid")], 1, 2, 50);
        var repository = new StubContentDirectoryRepository { NpcLookups = expected };
        var controller = new ContentDirectoryController(repository);
        using var cancellation = new CancellationTokenSource();

        var result = await controller.GetNpcTypes("interlude", new DirectoryRequest("  Humanoid  ", 2, 50), cancellation.Token);

        Assert.Same(expected, result);
        Assert.Equal("npc-types", repository.NpcLookupKind);
        Assert.Equal("Humanoid", repository.NpcLookupRequest?.Query);
        Assert.Equal(cancellation.Token, repository.NpcLookupToken);
    }

    [Fact]
    public async Task TrimsAndUpdatesNpcLookupDisplayNames()
    {
        var repository = new StubContentDirectoryRepository();
        var controller = new ContentDirectoryController(repository);

        var result = await controller.UpdateNpcRace(
            "c1", "DARK_ELF", new UpdateNpcLookupRequest("  Dark Elf  "), CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(new NpcLookupSummary("DARK_ELF", "Dark Elf"), response.Value);
        Assert.Equal("npc-races", repository.UpdatedKind);
        Assert.Equal("Dark Elf", repository.UpdatedDisplayName);
    }

    [Fact]
    public async Task RejectsBlankNpcLookupDisplayNames()
    {
        var controller = new ContentDirectoryController(new StubContentDirectoryRepository());
        var result = await controller.UpdateNpcSex(
            "c1", "ETC", new UpdateNpcLookupRequest("  "), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task TrimsAndUpdatesSkillLookupDisplayNames()
    {
        var repository = new StubContentDirectoryRepository();
        var controller = new ContentDirectoryController(repository);

        var result = await controller.UpdateSkillTargetType(
            "c1", "AREA_CORPSE_MOB", new UpdateNpcLookupRequest("  Area Corpse Mob  "), CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(new SkillLookupSummary("AREA_CORPSE_MOB", "Area Corpse Mob"), response.Value);
        Assert.Equal("skill-target-types", repository.UpdatedKind);
        Assert.Equal("Area Corpse Mob", repository.UpdatedDisplayName);
    }

    [Fact]
    public async Task GetsNpcDefinition()
    {
        var expected = Npc();
        var repository = new StubContentDirectoryRepository { Npc = expected };
        var controller = new ContentDirectoryController(repository);

        var result = await controller.GetNpc("c1", 100, CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, response.Value);
        Assert.Equal(100, repository.NpcId);
    }

    [Fact]
    public async Task TrimsAndUpdatesNpcDefinition()
    {
        var expected = Npc();
        var repository = new StubContentDirectoryRepository { UpdatedNpc = expected };
        var controller = new ContentDirectoryController(repository);

        var result = await controller.UpdateNpc(
            "c1", 100,
            new UpdateNpcRequest("  Goblin  ", 10, "  Monster  ", "  HUMANOID  ", "  MALE  "),
            CancellationToken.None);

        var response = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, response.Value);
        Assert.Equal("Goblin", repository.UpdatedNpcName);
        Assert.Equal((short)10, repository.UpdatedNpcLevel);
        Assert.Equal("Monster", repository.UpdatedNpcTypeName);
        Assert.Equal("HUMANOID", repository.UpdatedNpcRaceName);
        Assert.Equal("MALE", repository.UpdatedNpcSexName);
    }

    [Fact]
    public async Task AllowsClearingNpcRaceAndRejectsBlankName()
    {
        var repository = new StubContentDirectoryRepository { UpdatedNpc = Npc() };
        var controller = new ContentDirectoryController(repository);

        var clearRace = await controller.UpdateNpc(
            "c1", 100, new UpdateNpcRequest("Goblin", 10, "Monster", "  ", "MALE"), CancellationToken.None);
        Assert.IsType<OkObjectResult>(clearRace.Result);
        Assert.Null(repository.UpdatedNpcRaceName);

        var blankName = await controller.UpdateNpc(
            "c1", 100, new UpdateNpcRequest(" ", 10, "Monster", null, "MALE"), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(blankName.Result);
    }

    [Fact]
    public async Task DelegatesStructuredItemAttackGeometryAndRejectsNegativeDimensions()
    {
        var repository = new StubContentDirectoryRepository();
        var controller = new ContentDirectoryController(repository);
        var request = new UpdateItemRequest(
            "Crescent Moon Bow", null, null, null, null, null, null, null,
            null, new UpdateItemAttackGeometryRequest(0, 0, 10, 0));

        var result = await controller.UpdateItem("c1", "weapon", 3028, request, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
        Assert.Equal(request.AttackGeometry, repository.UpdatedItemRequest?.AttackGeometry);

        var invalid = await controller.UpdateItem("c1", "weapon", 3028, request with
        {
            AttackGeometry = new UpdateItemAttackGeometryRequest(0, 0, -1, 0)
        }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(invalid.Result);
    }

    [Fact]
    public async Task NormalizesItemHandlerBeforeUpdatingItem()
    {
        var repository = new StubContentDirectoryRepository();
        var controller = new ContentDirectoryController(repository);

        await controller.UpdateItem(
            "c1", "etc", 1660,
            new UpdateItemRequest("Cursed Bone", "  none  ", null, null, null, null, null, null,
                "  ItemSkills  ", null),
            CancellationToken.None);

        Assert.Equal("none", repository.UpdatedItemRequest?.ItemActionName);
        Assert.Equal("ItemSkills", repository.UpdatedItemRequest?.HandlerName);
    }

    [Fact]
    public async Task ValidatesAndDelegatesItemConditions()
    {
        var expected = new ItemConditionSummary(1518, false, null, "DWARF,HUMAN", "WOLF");
        var repository = new StubContentDirectoryRepository { UpdatedItemCondition = expected };
        var controller = new ContentDirectoryController(repository);
        var request = new UpdateItemConditionRequest(1518, false, null, ["human", "dwarf"], ["wolf"]);

        var result = await controller.UpdateItemCondition("c1", "etc", 57, request, CancellationToken.None);

        Assert.Equal(expected, Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal(request, repository.UpdatedItemConditionRequest);
        var invalid = await controller.UpdateItemCondition("c1", "etc", 57, request with { PlayerRaces = [], PlayerCategoryTypes = [] }, CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(invalid.Result);
    }

    [Fact]
    public async Task ValidatesAndNormalizesItemSkillMutations()
    {
        var expected = new ItemSkillSummary(3005, 1, "Bleed", "ON_CRITICAL_SKILL", "On critical skill", 50);
        var repository = new StubContentDirectoryRepository { CreatedItemSkill = expected };
        var controller = new ContentDirectoryController(repository);

        var created = await controller.CreateItemSkill(
            "c1", "weapon", 3028, new CreateItemSkillRequest(3005, 1, "  ON_CRITICAL_SKILL  ", 50), CancellationToken.None);

        var response = Assert.IsType<CreatedAtActionResult>(created.Result);
        Assert.Equal(expected, response.Value);
        Assert.Equal("ON_CRITICAL_SKILL", repository.CreatedItemSkillRequest?.ItemSkillTypeName);

        var invalid = await controller.CreateItemSkill(
            "c1", "weapon", 3028, new CreateItemSkillRequest(3005, 1, null, 101), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(invalid.Result);

        var invalidPrimary = await controller.SetItemPrimarySkill(
            "c1", "etc", 3028, new SetItemPrimarySkillRequest(0, 1), CancellationToken.None);
        Assert.IsType<BadRequestObjectResult>(invalidPrimary.Result);
    }

    [Fact]
    public async Task DeletesDefinitionsAndReportsDependencyConflicts()
    {
        var deleted = await new ContentDirectoryController(new StubContentDirectoryRepository { NpcDeleted = true })
            .DeleteNpc("c1", 100, CancellationToken.None);
        Assert.IsType<NoContentResult>(deleted);

        var conflict = await new ContentDirectoryController(new StubContentDirectoryRepository
        {
            NpcDeleteException = new ContentDeleteConflictException("NPC definitions", 2)
        }).DeleteNpc("c1", 100, CancellationToken.None);
        var response = Assert.IsType<ConflictObjectResult>(conflict);
        var problem = Assert.IsType<ProblemDetails>(response.Value);
        Assert.Equal("This record is used by 2 NPC definitions.", problem.Detail);
    }

    private static NpcSummary Npc() => new(100, 1, 10, "Goblin", "Monster", "Monster", "HUMANOID", "Humanoid", "MALE", "Male", false);

    private sealed class StubContentDirectoryRepository : IContentDirectoryRepository
    {
        public Task<ItemSetDirectoryPage> SearchItemSetsAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken) =>
            Task.FromResult(new ItemSetDirectoryPage([], 0, request.Page, request.PageSize));
        public Task<ItemSetSummary?> GetItemSetAsync(string gameVersion, int setId, CancellationToken cancellationToken) =>
            Task.FromResult<ItemSetSummary?>(null);
        public Task<ItemSetSummary?> UpdateItemSetAsync(string gameVersion, int setId, UpdateItemSetRequest request, CancellationToken cancellationToken) =>
            Task.FromResult<ItemSetSummary?>(null);
        public ItemDirectoryRequest? ItemRequest { get; private set; }
        public string? ItemFamily { get; private set; }
        public Task<ItemDirectoryPage> SearchItemsAsync(string gameVersion, string family, ItemDirectoryRequest request, CancellationToken cancellationToken)
        {
            ItemFamily = family;
            ItemRequest = request;
            return Task.FromResult(new ItemDirectoryPage([], 0, request.Page, request.PageSize));
        }
        public Task<ItemDetailSummary?> GetItemAsync(string gameVersion, string family, int id, CancellationToken cancellationToken) => Task.FromResult<ItemDetailSummary?>(null);
        public Task<ItemSummary?> UpdateItemAsync(string gameVersion, string family, int id, UpdateItemRequest request, CancellationToken cancellationToken)
        {
            UpdatedItemRequest = request;
            return Task.FromResult<ItemSummary?>(null);
        }
        public Task<bool> DeleteItemAsync(string gameVersion, string family, int id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<ItemConditionSummary?> UpdateItemConditionAsync(string gameVersion, string family, int itemId, UpdateItemConditionRequest request, CancellationToken cancellationToken)
        {
            UpdatedItemConditionRequest = request;
            return Task.FromResult(UpdatedItemCondition);
        }
        public Task<bool> DeleteItemConditionAsync(string gameVersion, string family, int itemId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<ItemPrimarySkillSummary?> SetItemPrimarySkillAsync(string gameVersion, string family, int itemId, SetItemPrimarySkillRequest request, CancellationToken cancellationToken) => Task.FromResult<ItemPrimarySkillSummary?>(null);
        public Task<bool> ClearItemPrimarySkillAsync(string gameVersion, string family, int itemId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<ItemSkillSummary?> CreateItemSkillAsync(string gameVersion, string family, int itemId, CreateItemSkillRequest request, CancellationToken cancellationToken)
        {
            CreatedItemSkillRequest = request;
            return Task.FromResult(CreatedItemSkill);
        }
        public Task<ItemSkillSummary?> UpdateItemSkillAsync(string gameVersion, string family, int itemId, int skillId, short skillLevel, UpdateItemSkillRequest request, CancellationToken cancellationToken) => Task.FromResult<ItemSkillSummary?>(null);
        public Task<bool> DeleteItemSkillAsync(string gameVersion, string family, int itemId, int skillId, short skillLevel, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<DirectoryPage<ItemTypeSummary>> SearchItemTypesAsync(string gameVersion, DirectoryRequest request, CancellationToken cancellationToken) => Task.FromResult(new DirectoryPage<ItemTypeSummary>([], 0, request.Page, request.PageSize));
        public Task<DirectoryPage<ItemLookupSummary>> SearchItemLookupsAsync(string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken) => Task.FromResult(new DirectoryPage<ItemLookupSummary>([], 0, request.Page, request.PageSize));
        public Task<ItemLookupSummary?> UpdateItemLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken) => Task.FromResult<ItemLookupSummary?>(new(name, displayName));
        public Task<bool> DeleteItemLookupAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken) => Task.FromResult(false);
        public NpcDirectoryPage Npcs { get; init; } = new([], 0, 1, 25);
        public DirectoryPage<NpcLookupSummary> NpcLookups { get; init; } = new([], 0, 1, 25);
        public NpcSummary? Npc { get; init; }
        public NpcSummary? UpdatedNpc { get; init; }
        public bool NpcDeleted { get; init; }
        public Exception? NpcDeleteException { get; init; }
        public string? NpcQuery { get; private set; }
        public int NpcPage { get; private set; }
        public int NpcPageSize { get; private set; }
        public CancellationToken NpcToken { get; private set; }
        public NpcDirectoryRequest? NpcRequest { get; private set; }
        public string? SkillQuery { get; private set; }
        public int SkillId { get; private set; }
        public SkillSummary? Skill { get; init; }
        public string? NpcLookupKind { get; private set; }
        public DirectoryRequest? NpcLookupRequest { get; private set; }
        public CancellationToken NpcLookupToken { get; private set; }
        public string? UpdatedKind { get; private set; }
        public string? UpdatedDisplayName { get; private set; }
        public int NpcId { get; private set; }
        public string? UpdatedNpcName { get; private set; }
        public short UpdatedNpcLevel { get; private set; }
        public string? UpdatedNpcTypeName { get; private set; }
        public string? UpdatedNpcRaceName { get; private set; }
        public string? UpdatedNpcSexName { get; private set; }
        public UpdateItemRequest? UpdatedItemRequest { get; private set; }
        public UpdateItemConditionRequest? UpdatedItemConditionRequest { get; private set; }
        public ItemConditionSummary? UpdatedItemCondition { get; init; }
        public CreateItemSkillRequest? CreatedItemSkillRequest { get; private set; }
        public ItemSkillSummary? CreatedItemSkill { get; init; }

        public Task<NpcDirectoryPage> SearchNpcsAsync(string gameVersion, NpcDirectoryRequest request, CancellationToken cancellationToken)
        {
            NpcRequest = request;
            NpcQuery = request.Query;
            NpcPage = request.Page;
            NpcPageSize = request.PageSize;
            NpcToken = cancellationToken;
            return Task.FromResult(Npcs);
        }

        public Task<NpcSummary?> GetNpcAsync(string gameVersion, int id, CancellationToken cancellationToken)
        {
            NpcId = id;
            return Task.FromResult(Npc);
        }

        public Task<NpcSummary?> UpdateNpcAsync(
            string gameVersion, int id, string name, short level, string npcTypeName, string? npcRaceName,
            string npcSexName, CancellationToken cancellationToken)
        {
            NpcId = id;
            UpdatedNpcName = name;
            UpdatedNpcLevel = level;
            UpdatedNpcTypeName = npcTypeName;
            UpdatedNpcRaceName = npcRaceName;
            UpdatedNpcSexName = npcSexName;
            return Task.FromResult(UpdatedNpc);
        }
        public Task<bool> DeleteNpcAsync(string gameVersion, int id, CancellationToken cancellationToken)
        {
            if (NpcDeleteException is not null) throw NpcDeleteException;
            return Task.FromResult(NpcDeleted);
        }

        public Task<DirectoryPage<NpcLookupSummary>> SearchNpcLookupsAsync(
            string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken)
        {
            NpcLookupKind = kind;
            NpcLookupRequest = request;
            NpcLookupToken = cancellationToken;
            return Task.FromResult(NpcLookups);
        }

        public Task<NpcLookupSummary?> UpdateNpcLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken)
        {
            UpdatedKind = kind;
            UpdatedDisplayName = displayName;
            return Task.FromResult<NpcLookupSummary?>(new(name, displayName));
        }
        public Task<bool> DeleteNpcLookupAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlyList<PlayerClassSummary>> GetPlayerClassesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlayerClassSummary>>([]);
        public Task<PlayerClassSummary?> UpdatePlayerClassAsync(string gameVersion, int id, UpdatePlayerClassRequest request, CancellationToken cancellationToken) => Task.FromResult<PlayerClassSummary?>(null);
        public Task<bool> DeletePlayerClassAsync(string gameVersion, int id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<DirectoryPage<PlayerLookupSummary>> SearchPlayerLookupsAsync(string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken) => Task.FromResult(new DirectoryPage<PlayerLookupSummary>([], 0, request.Page, request.PageSize));
        public Task<PlayerLookupSummary?> UpdatePlayerLookupNameAsync(string gameVersion, string kind, int id, string name, CancellationToken cancellationToken) => Task.FromResult<PlayerLookupSummary?>(null);
        public Task<bool> DeletePlayerLookupAsync(string gameVersion, string kind, int id, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<DirectoryPage<PlayerAppearanceSummary>> SearchPlayerAppearancesAsync(string gameVersion, string kind, PlayerAppearanceDirectoryRequest request, CancellationToken cancellationToken) => Task.FromResult(new DirectoryPage<PlayerAppearanceSummary>([], 0, request.Page, request.PageSize));
        public Task<PlayerAppearanceSummary?> UpdatePlayerAppearanceNameAsync(string gameVersion, string kind, int id, int playerRaceId, int playerSexId, string name, CancellationToken cancellationToken) => Task.FromResult<PlayerAppearanceSummary?>(null);
        public Task<bool> DeletePlayerAppearanceAsync(string gameVersion, string kind, int id, int playerRaceId, int playerSexId, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<SkillDirectoryPage> SearchSkillsAsync(string gameVersion, string query, int page, int pageSize, CancellationToken cancellationToken)
        {
            SkillQuery = query;
            return Task.FromResult(new SkillDirectoryPage([], 0, page, pageSize));
        }
        public Task<SkillSummary?> GetSkillAsync(string gameVersion, int id, CancellationToken cancellationToken)
        {
            SkillId = id;
            return Task.FromResult(Skill);
        }
        public Task<SkillSummary?> UpdateSkillAsync(string gameVersion, int id, UpdateSkillRequest request, CancellationToken cancellationToken) => Task.FromResult<SkillSummary?>(null);
        public Task<bool> DeleteSkillAsync(string gameVersion, int id, CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<DirectoryPage<SkillLookupSummary>> SearchSkillLookupsAsync(string gameVersion, string kind, DirectoryRequest request, CancellationToken cancellationToken) => Task.FromResult(new DirectoryPage<SkillLookupSummary>([], 0, request.Page, request.PageSize));
        public Task<SkillLookupSummary?> UpdateSkillLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken)
        {
            UpdatedKind = kind;
            UpdatedDisplayName = displayName;
            return Task.FromResult<SkillLookupSummary?>(new(name, displayName));
        }
        public Task<bool> DeleteSkillLookupAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
