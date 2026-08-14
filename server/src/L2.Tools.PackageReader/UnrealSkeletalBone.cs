using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealSkeletalBone(
    string Name,
    int ParentIndex,
    Quaternion Orientation,
    Vector3 Position);
