namespace L2.Studio.Services;

internal sealed record MapManifest(
    int SchemaVersion,
    string Name,
    string FileName,
    string SourceHash,
    int Protocol,
    MapEnvironmentManifestEntry Environment,
    IReadOnlyList<MapTerrainManifestEntry> Terrains,
    IReadOnlyList<MapActorManifestEntry> Actors,
    IReadOnlyList<MapLightManifestEntry> Lights,
    IReadOnlyList<MapWaterVolumeManifestEntry> WaterVolumes,
    IReadOnlyList<SkyZoneManifestEntry> SkyZones,
    IReadOnlyList<MapBspMeshManifestEntry> BspMeshes,
    IReadOnlyDictionary<string, int> UnrepresentedObjectClasses,
    IReadOnlyList<string> GpuTextureFormats);
