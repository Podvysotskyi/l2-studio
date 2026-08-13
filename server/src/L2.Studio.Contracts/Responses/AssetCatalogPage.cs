using System.Text.Json;

namespace L2.Studio.Contracts;

public sealed record AssetCatalogPage(
    AssetCatalogSummary Summary,
    IReadOnlyList<JsonElement> Groups,
    IReadOnlyList<JsonElement> Items,
    long Total,
    int Page,
    int PageSize);
