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

public sealed record AssetArtifactFileSummary(
    string RelativePath,
    string PublicPath,
    string Role,
    string MediaType,
    long SizeBytes,
    string Sha256);

public sealed record AssetArtifactDependencySummary(
    string Kind,
    string DependencyKey,
    Guid? ResolvedArtifactId,
    string? ResolvedSourceKey,
    string? BuildFingerprint,
    bool IsResolved);

public sealed record AssetArtifactDetail(
    AssetArtifactSummary Artifact,
    IReadOnlyList<AssetArtifactFileSummary> Files,
    IReadOnlyList<AssetArtifactDependencySummary> Dependencies);

public sealed record AssetArtifactPage(
    IReadOnlyList<AssetArtifactSummary> Items,
    long Total,
    int Page,
    int PageSize);
