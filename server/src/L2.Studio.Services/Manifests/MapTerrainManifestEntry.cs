namespace L2.Studio.Services;

internal sealed record MapTerrainManifestEntry(
    string Name,
    MapVector Location,
    MapRotation Rotation,
    MapVector Scale,
    string? Heightmap,
    int HeightmapWidth,
    int HeightmapHeight,
    string? MeshUrl,
    IReadOnlyList<MapTerrainLayerManifestEntry> Layers,
    IReadOnlyList<string> ControlMapUrls,
    int ControlMapWidth,
    int ControlMapHeight,
    string ControlMapEncoding,
    int ControlMapArrayGroup,
    string MaterialStatus,
    string? MaterialError);
