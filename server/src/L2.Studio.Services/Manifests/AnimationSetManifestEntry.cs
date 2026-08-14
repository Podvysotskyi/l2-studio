namespace L2.Studio.Services;

internal sealed record AnimationSetManifestEntry(
    string ObjectName,
    string Url,
    string Sha256,
    int BoneCount,
    string SkeletonSignature,
    IReadOnlyList<AnimationClipManifestEntry> Clips);
