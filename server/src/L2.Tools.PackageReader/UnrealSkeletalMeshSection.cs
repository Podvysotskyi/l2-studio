namespace L2.Tools.PackageReader;

public sealed record UnrealSkeletalMeshSection(
    int FirstIndex,
    int IndexCount,
    UnrealObjectReference? Material);
