using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealSkyBackdrop(
    string Name,
    UnrealStaticMesh? Mesh,
    string? Error);
