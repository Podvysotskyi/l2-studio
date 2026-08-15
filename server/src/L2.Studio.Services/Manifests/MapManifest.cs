namespace L2.Studio.Services;

internal sealed record MapManifest(
    int SchemaVersion,
    string Name,
    string FileName,
    string SourceHash,
    int Protocol,
    MapLevelSummaryManifestEntry? Summary,
    MapEnvironmentManifestEntry Environment,
    IReadOnlyList<MapTerrainManifestEntry> Terrains,
    IReadOnlyList<MapActorManifestEntry> Actors,
    IReadOnlyList<MapPlayerStartManifestEntry> PlayerStarts,
    IReadOnlyList<MapLightManifestEntry> Lights,
    IReadOnlyList<MapWaterVolumeManifestEntry> WaterVolumes,
    IReadOnlyList<SkyZoneManifestEntry> SkyZones,
    IReadOnlyList<MapBspMeshManifestEntry> BspMeshes,
    IReadOnlyDictionary<string, int> UnrepresentedObjectClasses,
    IReadOnlyList<string> GpuTextureFormats);
