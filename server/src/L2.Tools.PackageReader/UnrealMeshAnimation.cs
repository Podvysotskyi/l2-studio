namespace L2.Tools.PackageReader;

public sealed record UnrealMeshAnimation(
    string Name,
    IReadOnlyList<UnrealAnimationBone> Bones,
    IReadOnlyList<UnrealAnimationClip> Clips);
