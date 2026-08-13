using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealModelData(
    string Name,
    IReadOnlyList<Vector3> Vectors,
    IReadOnlyList<Vector3> Points,
    IReadOnlyList<UnrealModelNode> Nodes,
    IReadOnlyList<UnrealModelSurface> Surfaces,
    IReadOnlyList<int> Vertices);
