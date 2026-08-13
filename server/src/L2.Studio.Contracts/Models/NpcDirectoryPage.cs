namespace L2.Studio.Contracts;

public sealed record NpcDirectoryPage(
    IReadOnlyList<NpcSummary> Items,
    long Total,
    int Page,
    int PageSize);
