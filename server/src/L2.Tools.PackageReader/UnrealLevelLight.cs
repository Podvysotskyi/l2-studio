using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealLevelLight(
    string Name,
    string ClassName,
    Vector3 Location,
    UnrealRotator Rotation,
    float Brightness,
    byte Hue,
    byte Saturation,
    float Radius,
    IReadOnlyDictionary<string, string>? Properties = null);
