using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealSkyZoneInfo(
    int Order,
    string Name,
    Vector3 Location,
    float DrawScale,
    float TexUPanSpeed,
    float TexVPanSpeed,
    IReadOnlyList<UnrealSkyZoneLensFlare> LensFlares);
