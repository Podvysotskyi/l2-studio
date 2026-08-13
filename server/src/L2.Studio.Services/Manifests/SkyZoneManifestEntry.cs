namespace L2.Studio.Services;

internal sealed record SkyZoneManifestEntry(
    int Order,
    string Name,
    MapVector Location,
    float DrawScale,
    float TexUPanSpeed,
    float TexVPanSpeed,
    IReadOnlyList<SkyZoneLensFlareManifestEntry> LensFlares);
