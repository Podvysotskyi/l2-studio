namespace L2.Studio.Contracts;

public sealed record StaleAssetSourceSummary(
    string SourceKey,
    IReadOnlyList<string> ResourceNames,
    DateTimeOffset StaleAt,
    IReadOnlyList<string> Reasons);
