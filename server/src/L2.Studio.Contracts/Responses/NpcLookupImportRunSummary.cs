namespace L2.Studio.Contracts;

public sealed record NpcLookupImportRunSummary(
    Guid Id,
    string Kind,
    string Mode,
    string Status,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int TotalCount,
    int InsertedCount,
    int ExistingCount,
    int RestoredCount,
    string? Error);
