using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealPlayerStart(
    string Name,
    Vector3 Location,
    UnrealRotator Rotation);
