namespace L2.Studio.Contracts;

public sealed record AssetReleaseResourceOption(
    long FileId,
    Guid ArtifactId,
    string Kind,
    string SourceKey,
    string Label,
    string PublicPath,
    string MediaType,
    IReadOnlyList<string> CameraSequences);
