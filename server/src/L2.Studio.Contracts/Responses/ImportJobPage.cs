namespace L2.Studio.Contracts.Responses;

public sealed record ImportJobPage(
    IReadOnlyList<ImportJobSummary> Items,
    long Total,
    int Page,
    int PageSize);
