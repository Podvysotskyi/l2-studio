using System.Numerics;

namespace L2.Tools.PackageReader;

public readonly record struct UnrealTerrainUvTransform(
    UnrealTerrainUvTransformRow U,
    UnrealTerrainUvTransformRow V);
