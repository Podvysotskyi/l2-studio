namespace L2.Studio.Services;

internal sealed record SceneManifest(
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
    IReadOnlyList<SkyBackdropManifestEntry> SkyBackdrops,
    IReadOnlyList<SceneObjectManifestEntry> Cameras,
    IReadOnlyList<SceneObjectManifestEntry> InterpolationPoints,
    IReadOnlyList<SceneObjectManifestEntry> SceneManagers,
    IReadOnlyList<SceneObjectManifestEntry> Actions,
    IReadOnlyList<SceneObjectManifestEntry> AmbientSounds,
    IReadOnlyList<SceneObjectManifestEntry> Effects,
    IReadOnlyDictionary<string, int> UnrepresentedObjectClasses,
    IReadOnlyList<string> GpuTextureFormats);
