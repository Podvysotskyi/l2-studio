namespace L2.Studio.Contracts;

public sealed record AssetImportRunSummary(
    Guid Id,
    string Kind,
    string TriggerType,
    string Status,
    string? RequestedSourceKey,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DiscoveryFinishedAt,
    DateTimeOffset? FinishedAt,
    int DiscoveredFileCount,
    int CompletedFileCount,
    int SucceededFileCount,
    int WarningFileCount,
    int FailedFileCount,
    int ReusedFileCount,
    string? Error);
