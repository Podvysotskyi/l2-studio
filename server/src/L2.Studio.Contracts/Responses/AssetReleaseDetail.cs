namespace L2.Studio.Contracts;

public sealed record AssetReleaseDetail(
    AssetReleaseSummary Release,
    AssetReleaseEntrypoints Entrypoints,
    IReadOnlyList<AssetReleaseValidationIssue> ValidationIssues,
    IReadOnlyList<AssetReleaseArtifactSummary> Artifacts,
    IReadOnlyList<AssetReleaseEventSummary> Events,
    string PointerStatus,
    string? PointerError);
