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

public sealed record AssetImportWorkItemSummary(
    Guid Id,
    Guid RunId,
    string ImportKind,
    string SourceKey,
    string? SourceHash,
    string? ArtifactFingerprint,
    string Status,
    int AttemptCount,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int TotalResourceCount,
    int ProcessedResourceCount,
    int SkippedResourceCount,
    int WarningCount,
    string? Error,
    DateTimeOffset? UnpublishedAt);

public sealed record StaleAssetSourceSummary(
    string SourceKey,
    IReadOnlyList<string> ResourceNames,
    DateTimeOffset StaleAt,
    IReadOnlyList<string> Reasons);

public sealed record AssetImportDiagnosticSummary(
    long Id,
    Guid RunId,
    Guid? WorkItemId,
    string Severity,
    string Code,
    string Stage,
    string? SourceKey,
    string? ObjectName,
    string Message,
    DateTimeOffset CreatedAt);

public sealed record AssetImportWorkItemPage(
    IReadOnlyList<AssetImportWorkItemSummary> Items,
    long Total,
    int Page,
    int PageSize);

public sealed record AssetImportDiagnosticPage(
    IReadOnlyList<AssetImportDiagnosticSummary> Items,
    long Total,
    int Page,
    int PageSize);
