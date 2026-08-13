using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealBrushGeometry(
    IReadOnlyList<Vector3> Positions,
    IReadOnlyList<Vector3> Normals,
    IReadOnlyList<ushort> Indices)
{
    public int TriangleCount => Indices.Count / 3;
}
