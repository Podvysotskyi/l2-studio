namespace L2.Studio.Services;

internal sealed record AnimationMeshManifestEntry(
    string PackageName,
    string ObjectName,
    string? Url,
    int VertexCount,
    int TriangleCount,
    int SectionCount,
    int BoneCount,
    string SkeletonSignature,
    string? AnimationSetName,
    string? AnimationUrl,
    IReadOnlyList<AnimationClipManifestEntry> Clips,
    int MaterialCount,
    string MaterialStatus,
    string? Sha256,
    string Status,
    string? Error,
    string SourceKey);
