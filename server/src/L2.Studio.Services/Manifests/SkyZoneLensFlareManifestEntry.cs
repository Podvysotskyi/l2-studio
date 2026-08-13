namespace L2.Studio.Services;

internal sealed record SkyZoneLensFlareManifestEntry(
    int Index,
    string TexturePackage,
    string TextureObject,
    string? TextureUrl,
    float Offset,
    float Scale);
