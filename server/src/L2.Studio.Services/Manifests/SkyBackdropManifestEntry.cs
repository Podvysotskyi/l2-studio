namespace L2.Studio.Services;

internal sealed record SkyBackdropManifestEntry(
    string Name,
    string? MeshUrl,
    string? SkyZone,
    float TexUPanSpeed,
    float TexVPanSpeed,
    bool Collision,
    string? Error);
