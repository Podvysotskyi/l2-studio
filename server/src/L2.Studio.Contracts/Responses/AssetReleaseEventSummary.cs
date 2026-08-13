namespace L2.Studio.Contracts;

public sealed record AssetReleaseEventSummary(
    long Id,
    string Action,
    DateTimeOffset OccurredAt);
