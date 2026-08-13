using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealTerrainLayer(
    int Index,
    UnrealObjectReference? Texture,
    UnrealObjectReference? AlphaMap,
    float UScale,
    float VScale,
    float UPan,
    float VPan,
    byte TextureMapAxis,
    float TextureRotation,
    UnrealRotator LayerRotation);
