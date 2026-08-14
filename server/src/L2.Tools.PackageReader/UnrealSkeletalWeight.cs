using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealSkeletalWeight(
    ushort Bone0,
    ushort Bone1,
    ushort Bone2,
    ushort Bone3,
    Vector4 Weights);
