namespace L2.Studio.Contracts;

public sealed record AssetReleasePage(
    IReadOnlyList<AssetReleaseSummary> Items,
    long Total,
    int Page,
    int PageSize);
