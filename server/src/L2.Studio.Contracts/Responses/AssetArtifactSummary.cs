namespace L2.Studio.Contracts;

public sealed record AssetArtifactSummary(
    Guid Id,
    string Kind,
    string SourceKey,
    string SourceHash,
    string RecipeVersion,
    string BuildFingerprint,
    string ContentHash,
    string OutputRoot,
    int SchemaVersion,
    int? Protocol,
    int FileCount,
    long SizeBytes,
    string IntegrityStatus,
    DateTimeOffset? LastVerifiedAt,
    DateTimeOffset CreatedAt,
    bool IsCurrent);
