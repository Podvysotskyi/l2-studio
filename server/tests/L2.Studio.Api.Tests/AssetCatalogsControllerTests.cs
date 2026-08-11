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
            "textures",
            new AssetCatalogRequest("stone", "Terrain", 2, 25),
            cancellation.Token);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(expected, ok.Value);
        Assert.Equal("textures", repository.SearchKind);
        Assert.Equal("stone", repository.SearchQuery);
        Assert.Equal("Terrain", repository.SearchGroupName);
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
            "unknown",
            new AssetCatalogRequest(),
            CancellationToken.None);
        var missing = await controller.Search(
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

        var result = await controller.Get("textures", "Stone", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var item = Assert.IsType<JsonElement>(ok.Value);
        Assert.Equal("Stone", item.GetProperty("name").GetString());
        Assert.Equal("textures", repository.ItemKind);
        Assert.Equal("Stone", repository.ItemName);
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
        public int SearchPage { get; private set; }
        public int SearchPageSize { get; private set; }
        public CancellationToken SearchToken { get; private set; }
        public string? ItemKind { get; private set; }
        public string? ItemName { get; private set; }

        public Task<IReadOnlyList<AssetCatalogSummary>> GetSummariesAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetCatalogSummary>>([]);

        public Task<AssetCatalogPage?> SearchAsync(
            string kind,
            string query,
            string? groupName,
            int page,
            int pageSize,
            CancellationToken cancellationToken)
        {
            SearchKind = kind;
            SearchQuery = query;
            SearchGroupName = groupName;
            SearchPage = page;
            SearchPageSize = pageSize;
            SearchToken = cancellationToken;
            return Task.FromResult(SearchResult);
        }

        public Task<JsonElement?> GetAsync(string kind, string name, CancellationToken cancellationToken)
        {
            ItemKind = kind;
            ItemName = name;
            return Task.FromResult(ItemResult);
        }
    }
}
