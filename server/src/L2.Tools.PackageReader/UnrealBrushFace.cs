using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealBrushFace(
    IReadOnlyList<int> PointIndices,
    Vector3 Normal);
