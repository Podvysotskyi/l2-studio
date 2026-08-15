namespace L2.Studio.Contracts;

public sealed record DirectoryPage<TItem>(
    IReadOnlyList<TItem> Items,
    long Total,
    int Page,
    int PageSize);
