namespace L2.Studio.Contracts;

public sealed record AssetArtifactDetail(
    AssetArtifactSummary Artifact,
    IReadOnlyList<AssetArtifactFileSummary> Files,
    IReadOnlyList<AssetArtifactDependencySummary> Dependencies);
