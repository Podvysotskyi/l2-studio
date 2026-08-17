namespace L2.Studio.Contracts;

public sealed record ItemSetDirectoryPage(
    IReadOnlyList<ItemSetSummary> Items,
    long Total,
    int Page,
    int PageSize);
