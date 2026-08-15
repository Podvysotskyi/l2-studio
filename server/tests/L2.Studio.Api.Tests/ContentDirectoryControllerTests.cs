using L2.Studio.Api.Controllers;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using Microsoft.AspNetCore.Mvc;
using L2.Studio.Repositories.Interfaces;
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
    public async Task DelegatesLookupRequestsToTheMatchingRepositoryMethod()
    {
        var expected = new[] { new NpcLookupSummary("Humanoid", "Humanoid") };
        var repository = new StubContentDirectoryRepository { NpcTypes = expected };
        var controller = new ContentDirectoryController(repository);
        using var cancellation = new CancellationTokenSource();

        var result = await controller.GetNpcTypes("interlude", cancellation.Token);

        Assert.Same(expected, result);
        Assert.Equal(cancellation.Token, repository.NpcTypesToken);
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

    private static NpcSummary Npc() => new(100, 1, 10, "Goblin", "Monster", "Monster", "HUMANOID", "Humanoid", "MALE", "Male", false);

    private sealed class StubContentDirectoryRepository : IContentDirectoryRepository
    {
        public Task<ItemDirectoryPage> SearchItemsAsync(string gameVersion, ItemDirectoryRequest request, CancellationToken cancellationToken) => Task.FromResult(new ItemDirectoryPage([], 0, request.Page, request.PageSize));
        public Task<ItemSummary?> GetItemAsync(string gameVersion, int id, CancellationToken cancellationToken) => Task.FromResult<ItemSummary?>(null);
        public Task<ItemSummary?> UpdateItemAsync(string gameVersion, int id, UpdateItemRequest request, CancellationToken cancellationToken) => Task.FromResult<ItemSummary?>(null);
        public Task<IReadOnlyList<ItemLookupSummary>> GetItemLookupsAsync(string gameVersion, string kind, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<ItemLookupSummary>>([]);
        public Task<ItemLookupSummary?> UpdateItemLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken) => Task.FromResult<ItemLookupSummary?>(new(name, displayName));
        public NpcDirectoryPage Npcs { get; init; } = new([], 0, 1, 25);
        public IReadOnlyList<NpcLookupSummary> NpcTypes { get; init; } = [];
        public NpcSummary? Npc { get; init; }
        public NpcSummary? UpdatedNpc { get; init; }
        public string? NpcQuery { get; private set; }
        public int NpcPage { get; private set; }
        public int NpcPageSize { get; private set; }
        public CancellationToken NpcToken { get; private set; }
        public NpcDirectoryRequest? NpcRequest { get; private set; }
        public string? SkillQuery { get; private set; }
        public CancellationToken NpcTypesToken { get; private set; }
        public string? UpdatedKind { get; private set; }
        public string? UpdatedDisplayName { get; private set; }
        public int NpcId { get; private set; }
        public string? UpdatedNpcName { get; private set; }
        public short UpdatedNpcLevel { get; private set; }
        public string? UpdatedNpcTypeName { get; private set; }
        public string? UpdatedNpcRaceName { get; private set; }
        public string? UpdatedNpcSexName { get; private set; }

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

        public Task<IReadOnlyList<NpcLookupSummary>> GetNpcTypesAsync(string gameVersion, CancellationToken cancellationToken)
        {
            NpcTypesToken = cancellationToken;
            return Task.FromResult(NpcTypes);
        }

        public Task<IReadOnlyList<NpcLookupSummary>> GetNpcRacesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<NpcLookupSummary>>([]);
        public Task<IReadOnlyList<NpcLookupSummary>> GetNpcSexesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<NpcLookupSummary>>([]);
        public Task<NpcLookupSummary?> UpdateNpcLookupDisplayNameAsync(string gameVersion, string kind, string name, string displayName, CancellationToken cancellationToken)
        {
            UpdatedKind = kind;
            UpdatedDisplayName = displayName;
            return Task.FromResult<NpcLookupSummary?>(new(name, displayName));
        }
        public Task<IReadOnlyList<PlayerClassSummary>> GetPlayerClassesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlayerClassSummary>>([]);
        public Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerRacesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlayerLookupSummary>>([]);
        public Task<IReadOnlyList<PlayerLookupSummary>> GetPlayerSexesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlayerLookupSummary>>([]);
        public Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerFacesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlayerAppearanceSummary>>([]);
        public Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerHairStylesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlayerAppearanceSummary>>([]);
        public Task<IReadOnlyList<PlayerAppearanceSummary>> GetPlayerHairColorsAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<PlayerAppearanceSummary>>([]);

        public Task<SkillDirectoryPage> SearchSkillsAsync(string gameVersion, string query, int page, int pageSize, CancellationToken cancellationToken)
        {
            SkillQuery = query;
            return Task.FromResult(new SkillDirectoryPage([], 0, page, pageSize));
        }

        public Task<IReadOnlyList<SkillLookupSummary>> GetSkillOperateTypesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillLookupSummary>>([]);
        public Task<IReadOnlyList<SkillLookupSummary>> GetSkillTargetTypesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillLookupSummary>>([]);
    }
}
