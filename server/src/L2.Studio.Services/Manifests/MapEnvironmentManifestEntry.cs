namespace L2.Studio.Services;

internal sealed record MapEnvironmentManifestEntry(
    MapColor AmbientColor,
    float AmbientBrightness,
    MapDistanceFog? DistanceFog);
