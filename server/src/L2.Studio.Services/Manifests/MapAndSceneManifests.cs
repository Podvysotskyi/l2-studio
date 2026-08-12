namespace L2.Studio.Services;

internal sealed record MapCatalogManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<MapCatalogEntry> Maps);

internal sealed record MapCatalogEntry(
    string Name,
    string FileName,
    string? ManifestUrl,
    int TerrainCount,
    int ActorCount,
    int WaterVolumeCount,
    string Sha256,
    string Status,
    string? Error);

internal sealed record MapPreviewCatalogManifest(
    int SchemaVersion,
    string Kind,
    string SourceHash,
    int RendererVersion,
    IReadOnlyList<MapPreviewCatalogEntry> Previews);

internal sealed record MapPreviewCatalogEntry(
    string Name,
    string MapSourceHash,
    string? ImageUrl,
    int Width,
    int Height,
    string Status,
    string? Error);

internal sealed record MapPreviewRenderMap(string Name, string MapSourceHash);

internal sealed record MapPreviewRenderResult(string Name, string? Sha256, string? Error);

internal sealed record SceneCatalogManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<SceneCatalogEntry> Scenes);

internal sealed record SceneCatalogEntry(
    string Name,
    string FileName,
    string? ManifestUrl,
    int TerrainCount,
    int ActorCount,
    int CinematicObjectCount,
    string Sha256,
    string Status,
    string? Error);

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

internal sealed record SkyZoneManifestEntry(
    int Order,
    string Name,
    MapVector Location,
    float DrawScale,
    float TexUPanSpeed,
    float TexVPanSpeed,
    IReadOnlyList<SkyZoneLensFlareManifestEntry> LensFlares);

internal sealed record SkyZoneLensFlareManifestEntry(
    int Index,
    string TexturePackage,
    string TextureObject,
    string? TextureUrl,
    float Offset,
    float Scale);

internal sealed record SkyBackdropManifestEntry(
    string Name,
    string? MeshUrl,
    string? SkyZone,
    float TexUPanSpeed,
    float TexVPanSpeed,
    bool Collision,
    string? Error);

internal sealed record ParticleNumberRange(float Min, float Max);
internal sealed record ParticleVectorRange(MapVector Min, MapVector Max);
internal sealed record ParticleColorCurveKey(float Time, MapColorWithAlpha Color);
internal sealed record ParticleSizeCurveKey(float Time, float RelativeSize);
internal sealed record MapColorWithAlpha(float R, float G, float B, float A);
internal sealed record ParticleTextureSubdivisions(int U, int V, bool Random);
internal sealed record ParticleSpriteSettings(
    string DirectionMode,
    string StartLocationShape,
    ParticleNumberRange SphereRadius,
    string RotationSource,
    int ColorScaleRepeats);
internal sealed record ParticleBeamEndPoint(ParticleVectorRange Offset, float Weight);
internal sealed record ParticleBeamSettings(
    string EndPointMode,
    IReadOnlyList<ParticleBeamEndPoint> EndPoints,
    float TextureUScale,
    float TextureVScale,
    int RotatingSheets);
internal sealed record ParticleEmitterManifestEntry(
    string Kind,
    bool Enabled,
    int Capacity,
    string DrawStyle,
    float Opacity,
    ParticleNumberRange Lifetime,
    ParticleVectorRange StartSize,
    ParticleVectorRange StartVelocity,
    ParticleVectorRange StartLocation,
    MapVector StartLocationOffset,
    MapVector Acceleration,
    float ParticlesPerSecond,
    bool SpinParticles,
    ParticleNumberRange Spin,
    MapVector SpinDirection,
    ParticleTextureSubdivisions TextureSubdivisions,
    IReadOnlyList<ParticleSizeCurveKey> SizeCurve,
    IReadOnlyList<ParticleColorCurveKey> ColorCurve,
    float WarmupTime,
    float WarmupTicksPerSecond,
    ParticleSpriteSettings? Sprite,
    ParticleBeamSettings? Beam,
    IReadOnlyList<string> Diagnostics);

internal sealed record SceneObjectManifestEntry(
    int Order,
    string Name,
    string ClassName,
    MapVector Location,
    MapRotation Rotation,
    float Duration,
    string? Target,
    IReadOnlyDictionary<string, string> Properties,
    string? Owner = null,
    string? ResourceUrl = null,
    ParticleEmitterManifestEntry? Particle = null,
    string? Diagnostic = null);

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

internal sealed record MapTerrainUvTransform(
    MapTerrainUvTransformRow U,
    MapTerrainUvTransformRow V);

internal sealed record MapTerrainUvTransformRow(
    float X,
    float Y,
    float Z,
    float Offset);

internal sealed record MapActorManifestEntry(
    string Name,
    string ClassName,
    MapVector Location,
    MapRotation Rotation,
    MapVector PrePivot,
    float DrawScale,
    MapVector DrawScale3D,
    string? MeshPackage,
    string? MeshObject,
    string? MeshUrl,
    MapVertexLightingReference? VertexLighting);

internal sealed record MapVertexLightingReference(
    string Url,
    int TextureWidth,
    int TextureHeight,
    int TexelOffset,
    int VertexCount);

internal sealed record MapEnvironmentManifestEntry(
    MapColor AmbientColor,
    float AmbientBrightness,
    MapDistanceFog? DistanceFog);

internal sealed record MapDistanceFog(
    MapColor Color,
    float Start,
    float End);

internal sealed record MapColor(float R, float G, float B);

internal sealed record MapLightManifestEntry(
    string Name,
    string ClassName,
    MapVector Location,
    MapRotation Rotation,
    float Brightness,
    byte Hue,
    byte Saturation,
    float Radius,
    IReadOnlyDictionary<string, string>? Properties = null,
    string? ResourceUrl = null);

internal sealed record MapWaterVolumeManifestEntry(
    string Name,
    string ClassName,
    string? BrushName,
    MapVector Location,
    MapRotation Rotation,
    MapVector PrePivot,
    float DrawScale,
    MapVector DrawScale3D,
    string? MeshUrl,
    int VertexCount,
    int TriangleCount,
    string Status,
    string? Error);

internal sealed record MapBspMeshManifestEntry(
    string Name,
    string ModelName,
    string Role,
    string? SkyZone,
    IReadOnlyList<string> WaterVolumeNames,
    string? MeshUrl,
    int VertexCount,
    int TriangleCount,
    int SurfaceCount,
    int MaterialCount,
    int ResolvedMaterialCount,
    string MaterialStatus,
    uint PolyFlags,
    int SplitterNodeCount,
    int InvisibleSurfaceCount,
    int PortalSurfaceCount,
    int FakeBackdropSurfaceCount,
    int MalformedSurfaceCount,
    int UnresolvedMaterialReferenceCount,
    string? Error);

internal sealed record MapVector(float X, float Y, float Z);
internal sealed record MapRotation(int Pitch, int Yaw, int Roll);
