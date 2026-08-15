namespace L2.Studio.Contracts.Responses;

public sealed record ItemImportRunSummary(
    Guid Id, string Mode, string Status, DateTimeOffset RequestedAt, DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt, int TotalCount, int InsertedCount, int ExistingCount, int RestoredCount, string? Error);
