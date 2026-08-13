using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealWaterVolume(
    string Name,
    string ClassName,
    Vector3 Location,
    UnrealRotator Rotation,
    Vector3 PrePivot,
    float DrawScale,
    Vector3 DrawScale3D,
    UnrealObjectReference? Brush,
    UnrealBrushGeometry? Geometry,
    string? Error);
