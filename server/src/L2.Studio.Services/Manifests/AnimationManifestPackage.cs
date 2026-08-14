namespace L2.Studio.Services;

internal sealed record AnimationManifestPackage(
    string Name,
    string FileName,
    string Sha256,
    int SkeletalMeshCount,
    int AnimationSetCount,
    int ClipCount,
    int NotifyCount,
    int UnsupportedVertexMeshCount,
    string SourceKey);
