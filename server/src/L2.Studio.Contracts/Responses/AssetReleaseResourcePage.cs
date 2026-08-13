namespace L2.Studio.Contracts;

public sealed record AssetReleaseResourcePage(
    IReadOnlyList<AssetReleaseResourceOption> Items,
    long Total,
    int Page,
    int PageSize);
