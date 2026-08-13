using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealStaticMesh(
    string Name,
    IReadOnlyList<Vector3> Positions,
    IReadOnlyList<Vector3> Normals,
    IReadOnlyList<Vector2> TextureCoordinates,
    IReadOnlyList<ushort> Indices,
    IReadOnlyList<UnrealStaticMeshSection> Sections,
    IReadOnlyList<UnrealColor>? ColorStream0 = null,
    IReadOnlyList<UnrealColor>? ColorStream1 = null)
{
    public IReadOnlyList<UnrealColor> VertexColors0 { get; } = ColorStream0 ?? [];
    public IReadOnlyList<UnrealColor> VertexColors1 { get; } = ColorStream1 ?? [];
}
