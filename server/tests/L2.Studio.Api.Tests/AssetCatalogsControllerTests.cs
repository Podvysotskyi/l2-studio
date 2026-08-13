using System.Text.Json;
using L2.Studio.Api.Controllers;
using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;
using L2.Studio.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace L2.Studio.Api.Tests;

public sealed class AssetCatalogsControllerTests
{
    [Fact]
    public async Task SearchesSupportedCatalogsThroughTheRepository()
    {
        var expected = new AssetCatalogPage(Summary(), [], [], 10, 2, 25);
        var repository = new StubAssetCatalogRepository { SearchResult = expected };
        var controller = new AssetCatalogsController(repository);
        using var cancellation = new CancellationTokenSource();

        var result = await controller.Search(
            "interlude",
            "textures",
            new AssetCatalogRequest("stone", "Terrain", "textures", 2, 25),
            cancellation.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        Assert.Equal("textures", repository.SearchKind);
        Assert.Equal("stone", repository.SearchQuery);
        Assert.Equal("Terrain", repository.SearchGroupName);
        Assert.Equal("textures", repository.SearchOriginalFolder);
        Assert.Equal(2, repository.SearchPage);
        Assert.Equal(25, repository.SearchPageSize);
        Assert.Equal(cancellation.Token, repository.SearchToken);
    }

    [Fact]
    public async Task ReturnsNotFoundForUnknownOrMissingCatalogs()
    {
        var repository = new StubAssetCatalogRepository();
        var controller = new AssetCatalogsController(repository);

        var unsupported = await controller.Search(
            "interlude",
            "unknown",
            new AssetCatalogRequest(),
            CancellationToken.None);
        var missing = await controller.Search(
            "interlude",
            "textures",
            new AssetCatalogRequest(),
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(unsupported.Result);
        Assert.IsType<NotFoundResult>(missing.Result);
        Assert.Equal("textures", repository.SearchKind);
    }

    [Fact]
    public async Task ReturnsThePublishedCatalogItem()
    {
        using var document = JsonDocument.Parse("{\"name\":\"Stone\"}");
        var repository = new StubAssetCatalogRepository
        {
            ItemResult = document.RootElement.Clone()
        };
        var controller = new AssetCatalogsController(repository);

        var result = await controller.Get(
            "interlude", "textures", "Stone", "Textures/Stone.utx", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var item = Assert.IsType<JsonElement>(ok.Value);
        Assert.Equal("Stone", item.GetProperty("name").GetString());
        Assert.Equal("textures", repository.ItemKind);
        Assert.Equal("Stone", repository.ItemName);
        Assert.Equal("Textures/Stone.utx", repository.ItemSourceKey);
    }

    [Fact]
    public async Task ReturnsDiagnosticsForThePublishedCatalogItem()
    {
        var expected = new AssetCatalogDiagnosticPage(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Maps/16_25.unr",
            "succeeded_with_warnings",
            DateTimeOffset.UnixEpoch,
            [],
            17,
            2,
            25);
        var repository = new StubAssetCatalogRepository { DiagnosticResult = expected };
        var controller = new AssetCatalogsController(repository);
        using var cancellation = new CancellationTokenSource();

        var result = await controller.GetDiagnostics(
            "c1", "maps", "16_25", "Maps/16_25.unr", "warning", "BSP", 2, 25, cancellation.Token);

        Assert.Same(expected, Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("maps", repository.DiagnosticKind);
        Assert.Equal("16_25", repository.DiagnosticName);
        Assert.Equal("Maps/16_25.unr", repository.DiagnosticSourceKey);
        Assert.Equal("warning", repository.DiagnosticSeverity);
        Assert.Equal("BSP", repository.DiagnosticQuery);
        Assert.Equal(2, repository.DiagnosticPage);
        Assert.Equal(25, repository.DiagnosticPageSize);
        Assert.Equal(cancellation.Token, repository.DiagnosticToken);
    }

    [Fact]
    public async Task ValidatesCatalogDiagnosticFilters()
    {
        var repository = new StubAssetCatalogRepository();
        var controller = new AssetCatalogsController(repository);

        var severity = await controller.GetDiagnostics(
            "c1", "maps", "16_25", null, "notice", null, 1, 25, CancellationToken.None);
        var pageSize = await controller.GetDiagnostics(
            "c1", "maps", "16_25", null, null, null, 1, 101, CancellationToken.None);

        var severityProblem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(severity.Result).Value);
        var pageSizeProblem = Assert.IsType<ValidationProblemDetails>(
            Assert.IsType<BadRequestObjectResult>(pageSize.Result).Value);
        Assert.Equal("Severity must be warning or error.", Assert.Single(severityProblem.Errors["severity"]));
        Assert.Equal("Page size must be between 1 and 100.", Assert.Single(pageSizeProblem.Errors["pageSize"]));
        Assert.False(repository.DiagnosticsRequested);
    }

    private static AssetCatalogSummary Summary() => new(
        "textures", "Textures", "hash", 1, 1, 10, 9, 1, 2, DateTimeOffset.UnixEpoch);

    private sealed class StubAssetCatalogRepository : IAssetCatalogRepository
    {
        public AssetCatalogPage? SearchResult { get; init; }
        public JsonElement? ItemResult { get; init; }
        public AssetCatalogDiagnosticPage? DiagnosticResult { get; init; }
        public string? SearchKind { get; private set; }
        public string? SearchQuery { get; private set; }
        public string? SearchGroupName { get; private set; }
        public string? SearchOriginalFolder { get; private set; }
        public int SearchPage { get; private set; }
        public int SearchPageSize { get; private set; }
        public CancellationToken SearchToken { get; private set; }
        public string? ItemKind { get; private set; }
        public string? ItemName { get; private set; }
        public string? ItemSourceKey { get; private set; }
        public bool DiagnosticsRequested { get; private set; }
        public string? DiagnosticKind { get; private set; }
        public string? DiagnosticName { get; private set; }
        public string? DiagnosticSourceKey { get; private set; }
        public string? DiagnosticSeverity { get; private set; }
        public string? DiagnosticQuery { get; private set; }
        public int DiagnosticPage { get; private set; }
        public int DiagnosticPageSize { get; private set; }
        public CancellationToken DiagnosticToken { get; private set; }

        public Task<IReadOnlyList<AssetCatalogSummary>> GetSummariesAsync(string gameVersion, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetCatalogSummary>>([]);

        public Task<AssetCatalogPage?> SearchAsync(
            string gameVersion,
            string kind,
            string query,
            string? groupName,
            string? originalFolder,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            SearchKind = kind;
            SearchQuery = query;
            SearchGroupName = groupName;
            SearchOriginalFolder = originalFolder;
            SearchPage = page;
            SearchPageSize = pageSize;
            SearchToken = cancellationToken;
            return Task.FromResult(SearchResult);
        }

        public Task<JsonElement?> GetAsync(
            string gameVersion, string kind, string name, string? sourceKey, CancellationToken cancellationToken)
        {
            ItemKind = kind;
            ItemName = name;
            ItemSourceKey = sourceKey;
            return Task.FromResult(ItemResult);
        }

        public Task<AssetCatalogDiagnosticPage?> GetDiagnosticsAsync(
            string gameVersion,
            string kind,
            string name,
            string? sourceKey,
            string? severity,
            string? query,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            DiagnosticsRequested = true;
            DiagnosticKind = kind;
            DiagnosticName = name;
            DiagnosticSourceKey = sourceKey;
            DiagnosticSeverity = severity;
            DiagnosticQuery = query;
            DiagnosticPage = page;
            DiagnosticPageSize = pageSize;
            DiagnosticToken = cancellationToken;
            return Task.FromResult(DiagnosticResult);
        }

        public Task<AssetArtifactPage> GetArtifactsAsync(
            string gameVersion, string? kind, string? sourceKey, bool? current,
            string? integrityStatus, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(new AssetArtifactPage([], 0, page, pageSize));

        public Task<AssetArtifactDetail?> GetArtifactAsync(
            string gameVersion, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<AssetArtifactDetail?>(null);

        public Task<AssetArtifactDetail?> VerifyArtifactAsync(
            string gameVersion, Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<AssetArtifactDetail?>(null);
    }
}
