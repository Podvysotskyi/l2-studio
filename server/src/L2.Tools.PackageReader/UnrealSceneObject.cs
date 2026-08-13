using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealSceneObject(
    int Order,
    string Name,
    string ClassName,
    Vector3 Location,
    UnrealRotator Rotation,
    float Duration,
    UnrealObjectReference? Target,
    IReadOnlyDictionary<string, string> Properties,
    string? Owner = null);
