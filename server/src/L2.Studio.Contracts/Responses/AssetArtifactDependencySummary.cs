namespace L2.Studio.Contracts;

public sealed record AssetArtifactDependencySummary(
    string Kind,
    string DependencyKey,
    Guid? ResolvedArtifactId,
    string? ResolvedSourceKey,
    string? BuildFingerprint,
    bool IsResolved);
