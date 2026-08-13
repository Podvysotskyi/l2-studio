using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealTerrainInfo(
    string Name,
    Vector3 Location,
    UnrealRotator Rotation,
    Vector3 TerrainScale,
    UnrealCoordinateFrame ToWorld,
    UnrealCoordinateFrame ToHeightMap,
    UnrealObjectReference? TerrainMap,
    IReadOnlyList<UnrealTerrainLayer> Layers,
    bool CoordinateFramesDerived = false);
