namespace L2.Tools.PackageReader;

public sealed record UnrealAnimationPackage(
    IReadOnlyList<UnrealSkeletalMesh> SkeletalMeshes,
    IReadOnlyList<UnrealMeshAnimation> AnimationSets,
    int UnsupportedVertexMeshCount);
