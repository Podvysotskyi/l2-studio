using L2.Studio.Api.Controllers;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
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

        var result = await controller.Queue("c4", "npc-types", null, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        Assert.Equal($"/api/game-versions/c4/content/npc-types/imports/{run.Id}", accepted.Location);
        Assert.Equal("c4", repository.GameVersion);
        Assert.Equal("npc-types", repository.Kind);
        Assert.Equal("add_missing", repository.Mode);
    }

    [Fact]
    public async Task QueuesNpcSexImport()
    {
        var repository = new StubRepository { Queued = Summary() };
        var controller = new NpcLookupImportsController(repository);

        var result = await controller.Queue(
            "interlude", "npc-sexes", new NpcLookupImportRequest("restore_defaults"), CancellationToken.None);

        Assert.IsType<AcceptedResult>(result.Result);
        Assert.Equal("interlude", repository.GameVersion);
        Assert.Equal("npc-sexes", repository.Kind);
        Assert.Equal("restore_defaults", repository.Mode);
    }

    [Fact]
    public async Task ReturnsConflictWhenMatchingImportIsActive()
    {
        var controller = new NpcLookupImportsController(new StubRepository());
        var result = await controller.Queue("c1", "npc-races", null, CancellationToken.None);
        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task RejectsUnsupportedVersionsAndKinds()
    {
        var controller = new NpcLookupImportsController(new StubRepository());
        Assert.IsType<NotFoundResult>((await controller.Queue("high-five", "npc-races", null, CancellationToken.None)).Result);
        Assert.IsType<NotFoundResult>((await controller.Queue("c1", "npc-classes", null, CancellationToken.None)).Result);
    }

    [Fact]
    public async Task RejectsUnsupportedImportMode()
    {
        var controller = new NpcLookupImportsController(new StubRepository());

        var result = await controller.Queue(
            "c1", "npc-types", new NpcLookupImportRequest("replace_all"), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private static NpcLookupImportRunSummary Summary() => new(
        Guid.NewGuid(), "npc-types", "add_missing", "queued", DateTimeOffset.UtcNow,
        null, null, 0, 0, 0, 0, null);

    private sealed class StubRepository : INpcLookupImportRepository
    {
        public NpcLookupImportRunSummary? Queued { get; init; }
        public string? GameVersion { get; private set; }
        public string? Kind { get; private set; }
        public string? Mode { get; private set; }

        public Task<NpcLookupImportRunSummary?> QueueAsync(
            string gameVersion, string kind, string mode, CancellationToken cancellationToken)
        {
            GameVersion = gameVersion;
            Kind = kind;
            Mode = mode;
            return Task.FromResult(Queued);
        }

        public Task<IReadOnlyList<NpcLookupImportRunSummary>> GetRecentAsync(string gameVersion, string kind, int limit, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NpcLookupImportRunSummary>>([]);

        public Task<NpcLookupImportRunSummary?> GetAsync(string gameVersion, string kind, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<NpcLookupImportRunSummary?>(null);
    }
}
