namespace L2.Studio.Services;

internal sealed record MapTerrainLayerManifestEntry(
    int Index,
    string? TexturePackage,
    string? TextureObject,
    string? TextureUrl,
    int TextureWidth,
    int TextureHeight,
    int TextureArrayGroup,
    int TextureArrayLayer,
    string? AlphaPackage,
    string? AlphaObject,
    int ControlMapIndex,
    int ControlMapChannel,
    float UScale,
    float VScale,
    float UPan,
    float VPan,
    string TextureMapAxis,
    float TextureRotation,
    MapRotation LayerRotation,
    MapTerrainUvTransform UvTransform);
