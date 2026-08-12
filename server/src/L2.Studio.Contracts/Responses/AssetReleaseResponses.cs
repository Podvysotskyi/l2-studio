namespace L2.Studio.Contracts;

public sealed record AssetReleaseValidationIssue(string Code, string? Field, string Message);

public sealed record AssetReleaseEntrypoints(
    long? LoginSceneFileId,
    string? LoginScenePath,
    string? LoginCameraSequence,
    long? LoginMusicFileId,
    string? LoginMusicPath,
    long? PrimaryLogoFileId,
    string? PrimaryLogoPath,
    long? VersionLogoFileId,
    string? VersionLogoPath,
    long? LoadingArtworkFileId,
    string? LoadingArtworkPath,
    long? CharacterSelectionSceneFileId,
    string? CharacterSelectionScenePath,
    string? CharacterSelectionCameraSequence);

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

public sealed record AssetReleaseArtifactSummary(
    Guid ArtifactId,
    string Kind,
    string SourceKey,
    string BuildFingerprint,
    string IntegrityStatus,
    long SizeBytes,
    bool IsRoot);

public sealed record AssetReleaseEventSummary(
    long Id,
    string Action,
    DateTimeOffset OccurredAt);

public sealed record AssetReleaseDetail(
    AssetReleaseSummary Release,
    AssetReleaseEntrypoints Entrypoints,
    IReadOnlyList<AssetReleaseValidationIssue> ValidationIssues,
    IReadOnlyList<AssetReleaseArtifactSummary> Artifacts,
    IReadOnlyList<AssetReleaseEventSummary> Events,
    string PointerStatus,
    string? PointerError);

public sealed record AssetReleasePage(
    IReadOnlyList<AssetReleaseSummary> Items,
    long Total,
    int Page,
    int PageSize);

public sealed record AssetReleaseResourceOption(
    long FileId,
    Guid ArtifactId,
    string Kind,
    string SourceKey,
    string Label,
    string PublicPath,
    string MediaType,
    IReadOnlyList<string> CameraSequences);

public sealed record AssetReleaseResourcePage(
    IReadOnlyList<AssetReleaseResourceOption> Items,
    long Total,
    int Page,
    int PageSize);
