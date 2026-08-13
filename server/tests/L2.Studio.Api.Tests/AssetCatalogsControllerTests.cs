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

    private static AssetCatalogSummary Summary() => new(
        "textures", "Textures", "hash", 1, 1, 10, 9, 1, 2, DateTimeOffset.UnixEpoch);

    private sealed class StubAssetCatalogRepository : IAssetCatalogRepository
    {
        public AssetCatalogPage? SearchResult { get; init; }
        public JsonElement? ItemResult { get; init; }
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
