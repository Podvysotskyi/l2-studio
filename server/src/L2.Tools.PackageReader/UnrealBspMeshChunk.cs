using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealBspMeshChunk(
    string Name,
    UnrealStaticMesh Mesh,
    int SurfaceCount,
    UnrealPolyFlags RenderFlags,
    UnrealBspMeshRole Role,
    string? SkyZoneName = null,
    IReadOnlyList<string>? WaterVolumeNames = null);
