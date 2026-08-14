using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealSkeletalMesh(
    string Name,
    IReadOnlyList<Vector3> Positions,
    IReadOnlyList<Vector3> Normals,
    IReadOnlyList<Vector2> TextureCoordinates,
    IReadOnlyList<uint> Indices,
    IReadOnlyList<UnrealSkeletalWeight> Weights,
    IReadOnlyList<UnrealSkeletalBone> Bones,
    IReadOnlyList<UnrealSkeletalMeshSection> Sections,
    UnrealObjectReference? Animation,
    Vector3 MeshScale,
    Vector3 MeshOrigin,
    UnrealRotator RotationOrigin,
    string? Error = null);
