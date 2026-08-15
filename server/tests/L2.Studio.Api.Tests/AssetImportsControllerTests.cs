using L2.Studio.Api.Controllers;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Exceptions;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace L2.Studio.Api.Tests;

public sealed class AssetImportsControllerTests
{
    [Fact]
    public async Task QueuesAFullImportAndReturnsItsStatusLocation()
    {
        var run = Run();
        var repository = new StubAssetImportRepository { FullScanResult = run };
        var controller = new AssetImportsController(repository);
        using var cancellation = new CancellationTokenSource();

        var result = await controller.Queue("interlude", "textures", new AssetImportRequest(), cancellation.Token);

        var accepted = Assert.IsType<AcceptedResult>(result.Result);
        Assert.Same(run, accepted.Value);
        Assert.Equal($"/api/game-versions/interlude/assets/textures/imports/{run.Id}", accepted.Location);
        Assert.Equal("textures", repository.FullScanKind);
        Assert.Equal(cancellation.Token, repository.FullScanToken);
    }

    [Fact]
    public async Task RejectsUnknownOrConflictingFullImports()
    {
        var repository = new StubAssetImportRepository();
        var controller = new AssetImportsController(repository);

        var unknown = await controller.Queue("interlude", "unknown", null, CancellationToken.None);
        var conflict = await controller.Queue("interlude", "textures", null, CancellationToken.None);

        Assert.IsType<NotFoundResult>(unknown.Result);
        Assert.IsType<ConflictObjectResult>(conflict.Result);
        Assert.Equal("textures", repository.FullScanKind);
    }

    [Fact]
    public async Task RejectsAnimationImportsOutsideChronicleOne()
    {
        var repository = new StubAssetImportRepository();
        var controller = new AssetImportsController(repository);

        var result = await controller.Queue("interlude", AssetImportJobValues.Animations, null, CancellationToken.None);
        var problem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(result.Result).Value);

        Assert.Equal("Animation imports currently support Chronicle 1 only.", Assert.Single(problem.Errors["gameVersion"]));
        Assert.Null(repository.FullScanKind);
    }

    [Fact]
    public async Task MapsSingleFileValidationAndMissingSourceErrors()
    {
        var repository = new StubAssetImportRepository
        {
            FileException = new ArgumentException("The source must be a .utx file.")
        };
        var controller = new AssetImportsController(repository);

        var invalid = await controller.QueueFile("textures", "interlude", "Example.usx", false, CancellationToken.None);
        var invalidProblem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(invalid.Result).Value);
        Assert.Equal(
            "The source must be a .utx file.",
            Assert.Single(invalidProblem.Errors["fileName"]));

        repository.FileException = new AssetImportTargetNotFoundException("Missing.utx");
        var missing = await controller.QueueFile("textures", "interlude", "Missing.utx", false, CancellationToken.None);
        Assert.IsType<NotFoundObjectResult>(missing.Result);
    }

    [Fact]
    public async Task QueuesAnExactCatalogResourceSource()
    {
        var run = Run();
        var repository = new StubAssetImportRepository { ResourceResult = run };
        var controller = new AssetImportsController(repository);

        var result = await controller.QueueResource(
            "textures",
            "interlude",
            new AssetResourceImportRequest(
                "Stone",
                "Terrain",
                "Textures/Terrain.utx",
                true),
            CancellationToken.None);

        Assert.IsType<AcceptedResult>(result.Result);
        Assert.Equal("Textures/Terrain.utx", repository.ResourceSourceKey);
    }

    [Fact]
    public async Task ValidatesRecentImportLimitsBeforeCallingTheRepository()
    {
        var repository = new StubAssetImportRepository();
        var controller = new AssetImportsController(repository);

        var result = await controller.List("textures", "interlude", 101, CancellationToken.None);
        var problem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(result.Result).Value);

        Assert.Equal("Limit must be between 1 and 100.", Assert.Single(problem.Errors["limit"]));
        Assert.False(repository.RecentRequested);
    }

    [Fact]
    public async Task LoadsRecentImportsForSupportedKinds()
    {
        var expected = new[] { Run() };
        var repository = new StubAssetImportRepository { RecentResult = expected };
        var controller = new AssetImportsController(repository);

        var result = await controller.List("textures", "interlude", 10, CancellationToken.None);

        Assert.Same(expected, Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("textures", repository.RecentKind);
        Assert.Equal(10, repository.RecentLimit);
    }

    [Fact]
    public async Task RejectsNpcAppearanceImportsOutsideChronicleOne()
    {
        var repository = new StubAssetImportRepository();
        var controller = new AssetImportsController(repository);

        var result = await controller.Queue(
            "interlude", AssetImportJobValues.NpcAppearances, new AssetImportRequest(false), CancellationToken.None);
        var problem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(result.Result).Value);

        Assert.Equal("NPC appearance imports currently support Chronicle 1 only.",
            Assert.Single(problem.Errors["gameVersion"]));
    }

    [Fact]
    public async Task ValidatesWorkItemAndDiagnosticFilters()
    {
        var repository = new StubAssetImportRepository();
        var controller = new AssetImportsController(repository);
        var id = Guid.NewGuid();

        var workItems = await controller.GetWorkItems(
            "textures", "interlude", id, null, "invalid", null, null, 1, 50, CancellationToken.None);
        var diagnostics = await controller.GetDiagnostics(
            "textures", "interlude", id, null, "notice", null, null, null, null, null, 1, 50, CancellationToken.None);

        var workItemProblem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(workItems.Result).Value);
        var diagnosticProblem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(diagnostics.Result).Value);
        Assert.Equal("Work-item status is invalid.", Assert.Single(workItemProblem.Errors["status"]));
        Assert.Equal("Severity must be warning or error.", Assert.Single(diagnosticProblem.Errors["severity"]));
    }

    [Fact]
    public async Task ValidatesUnifiedWorkItemFiltersAndRunDiagnosticScope()
    {
        var repository = new StubAssetImportRepository();
        var controller = new AssetImportsController(repository);
        var id = Guid.NewGuid();

        var workItems = await controller.GetWorkItems(
            "textures", "interlude", id, null, null, "terrain", "notice", 1, 50, CancellationToken.None);
        var diagnostics = await controller.GetDiagnostics(
            "textures", "interlude", id, null, null, null, null, null, null, "file", 1, 50, CancellationToken.None);

        var workItemProblem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(workItems.Result).Value);
        var diagnosticProblem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(diagnostics.Result).Value);
        Assert.Equal("Diagnostic severity must be warning or error.", Assert.Single(workItemProblem.Errors["diagnosticSeverity"]));
        Assert.Equal("Diagnostic scope is invalid.", Assert.Single(diagnosticProblem.Errors["scope"]));
    }

    private static AssetImportRunSummary Run() => new(
        Guid.NewGuid(),
        AssetImportJobValues.Textures,
        AssetImportJobValues.FullScan,
        AssetImportJobValues.Queued,
        null,
        DateTimeOffset.UnixEpoch,
        null,
        null,
        null,
        0,
        0,
        0,
        0,
        0,
        0,
        null);

    private sealed class StubAssetImportRepository : IAssetImportRepository
    {
        public AssetImportRunSummary? FullScanResult { get; init; }
        public AssetImportRunSummary? ResourceResult { get; init; }
        public Exception? FileException { get; set; }
        public IReadOnlyList<AssetImportRunSummary> RecentResult { get; init; } = [];
        public string? FullScanKind { get; private set; }
        public CancellationToken FullScanToken { get; private set; }
        public bool RecentRequested { get; private set; }
        public string? RecentKind { get; private set; }
        public int RecentLimit { get; private set; }
        public string? ResourceSourceKey { get; private set; }

        public Task<AssetImportRunSummary?> QueueFullScanAsync(string gameVersion, string kind, bool force, CancellationToken cancellationToken)
        {
            FullScanKind = kind;
            FullScanToken = cancellationToken;
            return Task.FromResult(FullScanResult);
        }

        public Task<AssetImportRunSummary?> QueueSingleFileAsync(string gameVersion, string kind, string fileName, bool force, CancellationToken cancellationToken)
        {
            if (FileException is not null) throw FileException;
            return Task.FromResult<AssetImportRunSummary?>(null);
        }

        public Task<AssetImportRunSummary?> QueueResourceAsync(string gameVersion, string kind, string resourceName, string? packageName, string? sourceKey, bool force, CancellationToken cancellationToken)
        {
            ResourceSourceKey = sourceKey;
            return Task.FromResult(ResourceResult);
        }

        public Task<IReadOnlyList<StaleAssetSourceSummary>> GetStaleAsync(string gameVersion, string kind, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<StaleAssetSourceSummary>>([]);

        public Task<AssetImportRunSummary?> QueueStaleAsync(string gameVersion, string kind, CancellationToken cancellationToken) =>
            Task.FromResult<AssetImportRunSummary?>(null);

        public Task<IReadOnlyList<AssetImportRunSummary>> GetRecentAsync(string gameVersion, string kind, int limit, CancellationToken cancellationToken)
        {
            RecentRequested = true;
            RecentKind = kind;
            RecentLimit = limit;
            return Task.FromResult(RecentResult);
        }

        public Task<AssetImportRunSummary?> GetAsync(Guid id, string gameVersion, string kind, CancellationToken cancellationToken) => Task.FromResult<AssetImportRunSummary?>(null);
        public Task<AssetImportWorkItemPage?> GetWorkItemsAsync(Guid runId, string gameVersion, string kind, string? sourceKey, string? status, string? query, string? diagnosticSeverity, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult<AssetImportWorkItemPage?>(null);
        public Task<AssetImportDiagnosticPage?> GetDiagnosticsAsync(Guid runId, string gameVersion, string kind, string? sourceKey, string? severity, string? code, string? stage, string? workItemStatus, string? query, string? scope, int page, int pageSize, CancellationToken cancellationToken) => Task.FromResult<AssetImportDiagnosticPage?>(null);
    }
}
