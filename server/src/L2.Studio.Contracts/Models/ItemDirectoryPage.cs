namespace L2.Studio.Contracts;

public sealed record ItemDirectoryPage(IReadOnlyList<ItemSummary> Items, long Total, int Page, int PageSize);
