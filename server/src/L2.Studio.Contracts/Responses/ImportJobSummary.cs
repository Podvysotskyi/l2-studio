namespace L2.Studio.Contracts.Responses;

public sealed record ImportJobSummary(
    Guid Id,
    string Category,
    string Target,
    string Operation,
    string Status,
    string? RequestedSourceKey,
    bool Force,
    DateTimeOffset RequestedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? DiscoveryFinishedAt,
    DateTimeOffset? FinishedAt,
    int TotalCount,
    int CompletedCount,
    IReadOnlyList<ImportJobMetricSummary> Metrics,
    string? Error);
