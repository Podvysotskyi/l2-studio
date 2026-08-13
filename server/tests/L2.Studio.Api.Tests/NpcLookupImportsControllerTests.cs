using L2.Studio.Api.Controllers;
using L2.Studio.Contracts;
using L2.Studio.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace L2.Studio.Api.Tests;

public sealed class NpcLookupImportsControllerTests
{
    [Fact]
    public async Task QueuesSupportedVersionSpecificImport()
    {
        var run = Summary();
        var repository = new StubRepository { Queued = run };
        var controller = new NpcLookupImportsController(repository);

        var result = await controller.Queue("c4", "npc-types", CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        Assert.Equal($"/api/game-versions/c4/content/npc-types/imports/{run.Id}", accepted.Location);
        Assert.Equal("c4", repository.GameVersion);
        Assert.Equal("npc-types", repository.Kind);
    }

    [Fact]
    public async Task ReturnsConflictWhenMatchingImportIsActive()
    {
        var controller = new NpcLookupImportsController(new StubRepository());
        var result = await controller.Queue("c1", "npc-races", CancellationToken.None);
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task RejectsUnsupportedVersionsAndKinds()
    {
        var controller = new NpcLookupImportsController(new StubRepository());
        Assert.IsType<NotFoundResult>((await controller.Queue("high-five", "npc-races", CancellationToken.None)).Result);
        Assert.IsType<NotFoundResult>((await controller.Queue("c1", "npc-sexes", CancellationToken.None)).Result);
    }

    private static NpcLookupImportRunSummary Summary() => new(
        Guid.NewGuid(), "npc-types", "queued", DateTimeOffset.UtcNow,
        null, null, 0, 0, 0, null);

    private sealed class StubRepository : INpcLookupImportRepository
    {
        public NpcLookupImportRunSummary? Queued { get; init; }
        public string? GameVersion { get; private set; }
        public string? Kind { get; private set; }

        public Task<NpcLookupImportRunSummary?> QueueAsync(string gameVersion, string kind, CancellationToken cancellationToken)
        {
            GameVersion = gameVersion;
            Kind = kind;
            return Task.FromResult(Queued);
        }

        public Task<IReadOnlyList<NpcLookupImportRunSummary>> GetRecentAsync(string gameVersion, string kind, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NpcLookupImportRunSummary>>([]);

        public Task<NpcLookupImportRunSummary?> GetAsync(string gameVersion, string kind, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<NpcLookupImportRunSummary?>(null);
    }
}
