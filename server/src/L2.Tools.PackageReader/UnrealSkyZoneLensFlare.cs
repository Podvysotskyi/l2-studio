using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealSkyZoneLensFlare(
    int Index,
    UnrealObjectReference Texture,
    float Offset,
    float Scale);
