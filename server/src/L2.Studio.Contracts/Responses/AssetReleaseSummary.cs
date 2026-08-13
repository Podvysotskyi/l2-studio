namespace L2.Studio.Contracts;

public sealed record AssetReleaseSummary(
    Guid Id,
    string Name,
    string? Notes,
    string Status,
    string ValidationStatus,
    string SnapshotHash,
    int RootArtifactCount,
    int ArtifactCount,
    long SizeBytes,
    string? ManifestPath,
    string? ManifestHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? RetiredAt,
    bool IsActive,
    bool IsDesired);
