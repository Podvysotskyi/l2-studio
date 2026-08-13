namespace L2.Studio.Contracts;

public sealed record AssetImportWorkItemPage(
    IReadOnlyList<AssetImportWorkItemSummary> Items,
    long Total,
    int Page,
    int PageSize);
