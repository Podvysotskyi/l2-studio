using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealDistanceFog(
    UnrealColor Color,
    float Start,
    float End);
