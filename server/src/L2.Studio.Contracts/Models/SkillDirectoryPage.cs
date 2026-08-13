namespace L2.Studio.Contracts;

public sealed record SkillDirectoryPage(
    IReadOnlyList<SkillSummary> Items,
    long Total,
    int Page,
    int PageSize);
