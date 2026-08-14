using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealAnimationTrack(
    IReadOnlyList<Quaternion> Rotations,
    IReadOnlyList<Vector3> Translations,
    IReadOnlyList<float> Times);
