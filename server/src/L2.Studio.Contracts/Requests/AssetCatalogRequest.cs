namespace L2.Studio.Contracts.Requests;

public sealed record AssetCatalogRequest(
    string Query = "",
    string? PackageName = null,
    string? OriginalFolder = null,
    int Page = 1,
    int PageSize = 50);
