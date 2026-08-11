namespace L2.Studio.Contracts;

public sealed record AssetImportJobSummary(
    Guid Id,
    string Kind,
    string Status,
    string SourcePath,
    string? SourceHash,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? FinishedAt,
    int TotalCount,
    int ProcessedCount,
    int SkippedCount,
    IReadOnlyList<string> Warnings,
    string? Error);
