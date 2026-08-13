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
            new DirectoryRequest("  Goblin  ", 2, 50),
            cancellation.Token);

        Assert.Same(expected, result);
        Assert.Equal("Goblin", repository.NpcQuery);
        Assert.Equal(2, repository.NpcPage);
        Assert.Equal(50, repository.NpcPageSize);
        Assert.Equal(cancellation.Token, repository.NpcToken);
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

    private sealed class StubContentDirectoryRepository : IContentDirectoryRepository
    {
        public NpcDirectoryPage Npcs { get; init; } = new([], 0, 1, 25);
        public IReadOnlyList<NpcLookupSummary> NpcTypes { get; init; } = [];
        public string? NpcQuery { get; private set; }
        public int NpcPage { get; private set; }
        public int NpcPageSize { get; private set; }
        public CancellationToken NpcToken { get; private set; }
        public string? SkillQuery { get; private set; }
        public CancellationToken NpcTypesToken { get; private set; }
        public string? UpdatedKind { get; private set; }
        public string? UpdatedDisplayName { get; private set; }

        public Task<NpcDirectoryPage> SearchNpcsAsync(string gameVersion, string query, int page, int pageSize, CancellationToken cancellationToken)
        {
            NpcQuery = query;
            NpcPage = page;
            NpcPageSize = pageSize;
            NpcToken = cancellationToken;
            return Task.FromResult(Npcs);
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

        public Task<SkillDirectoryPage> SearchSkillsAsync(string gameVersion, string query, int page, int pageSize, CancellationToken cancellationToken)
        {
            SkillQuery = query;
            return Task.FromResult(new SkillDirectoryPage([], 0, page, pageSize));
        }

        public Task<IReadOnlyList<SkillLookupSummary>> GetSkillOperateTypesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillLookupSummary>>([]);
        public Task<IReadOnlyList<SkillLookupSummary>> GetSkillTargetTypesAsync(string gameVersion, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<SkillLookupSummary>>([]);
    }
}
