namespace L2.Studio.Contracts;

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
    int ErrorCount,
    string? Error,
    DateTimeOffset? UnpublishedAt);
