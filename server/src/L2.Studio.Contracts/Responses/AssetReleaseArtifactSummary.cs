namespace L2.Studio.Contracts;

public sealed record AssetReleaseArtifactSummary(
    Guid ArtifactId,
    string Kind,
    string SourceKey,
    string BuildFingerprint,
    string IntegrityStatus,
    long SizeBytes,
    bool IsRoot);
