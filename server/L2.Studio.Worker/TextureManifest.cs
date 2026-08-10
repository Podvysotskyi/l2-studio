namespace L2.Studio.Worker;

internal sealed record TextureManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<TextureManifestPackage> Packages,
    IReadOnlyList<TextureManifestEntry> Textures,
    IReadOnlyList<TextureMaterialManifestEntry>? Materials);

internal sealed record TextureManifestPackage(
    string Name,
    string FileName,
    string Sha256,
    int TextureCount,
    int MaterialCount);

internal sealed record TextureManifestEntry(
    string PackageName,
    string ObjectName,
    string? Url,
    int Width,
    int Height,
    string Format,
    string? Sha256,
    string Status,
    string? Error,
    string? GpuUrl = null,
    string? GpuSha256 = null,
    bool GpuCompressed = false,
    int MipCount = 0,
    TextureAnimationManifestEntry? Animation = null);

internal sealed record TextureAnimationManifestEntry(
    IReadOnlyList<string> FrameUrls,
    float MinFrameRate,
    float MaxFrameRate);

internal sealed record TextureMaterialReference(
    string PackageName,
    string ObjectName,
    string ClassName);

internal sealed record TextureMaterialColor(byte Red, byte Green, byte Blue, byte Alpha);

internal sealed record TextureMaterialManifestEntry(
    string PackageName,
    string ObjectName,
    string ClassName,
    TextureMaterialReference? Material,
    TextureMaterialReference? Diffuse,
    TextureMaterialReference? Opacity,
    TextureMaterialReference? SelfIllumination,
    byte OutputBlending,
    byte FrameBufferBlending,
    bool TwoSided,
    bool AlphaTest,
    byte AlphaRef,
    bool ZWrite,
    bool ZTest,
    TextureMaterialReference? Material2 = null,
    TextureMaterialReference? Mask = null,
    float PanRate = 0,
    float RotationRate = 0,
    byte CombineOperation = 0,
    byte AlphaOperation = 0,
    TextureMaterialReference? Detail = null,
    float DetailScale = 8,
    TextureMaterialColor? ModifierColor = null,
    byte UOscillationType = 0,
    byte VOscillationType = 0,
    float UOscillationRate = 0,
    float VOscillationRate = 0,
    float UOscillationAmplitude = 0,
    float VOscillationAmplitude = 0,
    float UOscillationPhase = 0,
    float VOscillationPhase = 0,
    bool TreatAsTwoSided = false,
    TextureMaterialReference? SelfIlluminationMask = null,
    TextureMaterialReference? Specular = null,
    TextureMaterialReference? SpecularityMask = null,
    bool PerformLightingOnSpecularPass = false,
    TextureMaterialColor? FadeColor1 = null,
    TextureMaterialColor? FadeColor2 = null,
    byte ColorFadeType = 0,
    float FadePeriod = 0,
    float FadePhase = 0,
    bool InvertMask = false,
    bool Modulate2X = false,
    bool Modulate4X = false);

internal sealed record TextureCatalogMetadata(IReadOnlyList<TextureMaterialManifestEntry> Materials);

internal sealed record MusicManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    IReadOnlyList<MusicManifestEntry> Tracks);

internal sealed record MusicManifestEntry(
    string Name,
    string FileName,
    string? Url,
    double? DurationSeconds,
    int? SampleRate,
    int? Channels,
    long SizeBytes,
    string? Sha256,
    string Status,
    string? Error);

internal sealed record SoundManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<SoundManifestEntry> Sounds);

internal sealed record SoundManifestEntry(
    string PackageName,
    string ObjectName,
    string Url,
    double DurationSeconds,
    int SampleRate,
    int Channels,
    long SizeBytes,
    string Sha256);

internal sealed record StaticMeshManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<StaticMeshManifestPackage> Packages,
    IReadOnlyList<StaticMeshManifestEntry> Meshes,
    IReadOnlyList<string>? GpuTextureFormats = null);

internal sealed record StaticMeshManifestPackage(
    string Name,
    string FileName,
    string Sha256,
    int MeshCount);

internal sealed record StaticMeshManifestEntry(
    string PackageName,
    string ObjectName,
    string? Url,
    int VertexCount,
    int TriangleCount,
    int SectionCount,
    int MaterialCount,
    int ResolvedMaterialCount,
    string MaterialStatus,
    string? MaterialError,
    string? Sha256,
    string Status,
    string? Error);

internal sealed record LevelCatalogManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<LevelCatalogEntry> Levels);

internal sealed record LevelCatalogEntry(
    string Name,
    string FileName,
    string? ManifestUrl,
    int TerrainCount,
    int ActorCount,
    int WaterVolumeCount,
    string Sha256,
    string Status,
    string? Error);

internal sealed record LevelPreviewCatalogManifest(
    int SchemaVersion,
    string Kind,
    string SourceHash,
    int RendererVersion,
    IReadOnlyList<LevelPreviewCatalogEntry> Previews);

internal sealed record LevelPreviewCatalogEntry(
    string Name,
    string LevelSourceHash,
    string? ImageUrl,
    int Width,
    int Height,
    string Status,
    string? Error);

internal sealed record LevelPreviewRenderLevel(string Name, string LevelSourceHash);

internal sealed record LevelPreviewRenderResult(string Name, string? Sha256, string? Error);

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

internal sealed record LevelManifest(
    int SchemaVersion,
    string Name,
    string FileName,
    string SourceHash,
    int Protocol,
    LevelEnvironmentManifestEntry Environment,
    IReadOnlyList<LevelTerrainManifestEntry> Terrains,
    IReadOnlyList<LevelActorManifestEntry> Actors,
    IReadOnlyList<LevelLightManifestEntry> Lights,
    IReadOnlyList<LevelWaterVolumeManifestEntry> WaterVolumes,
    IReadOnlyList<SkyZoneManifestEntry> SkyZones,
    IReadOnlyList<LevelBspMeshManifestEntry> BspMeshes,
    IReadOnlyDictionary<string, int> UnrepresentedObjectClasses,
    IReadOnlyList<string> GpuTextureFormats);

internal sealed record SceneManifest(
    int SchemaVersion,
    string Name,
    string FileName,
    string SourceHash,
    int Protocol,
    LevelEnvironmentManifestEntry Environment,
    IReadOnlyList<LevelTerrainManifestEntry> Terrains,
    IReadOnlyList<LevelActorManifestEntry> Actors,
    IReadOnlyList<LevelLightManifestEntry> Lights,
    IReadOnlyList<LevelWaterVolumeManifestEntry> WaterVolumes,
    IReadOnlyList<SkyZoneManifestEntry> SkyZones,
    IReadOnlyList<LevelBspMeshManifestEntry> BspMeshes,
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
    LevelVector Location,
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
internal sealed record ParticleVectorRange(LevelVector Min, LevelVector Max);
internal sealed record ParticleColorCurveKey(float Time, LevelColorWithAlpha Color);
internal sealed record ParticleSizeCurveKey(float Time, float RelativeSize);
internal sealed record LevelColorWithAlpha(float R, float G, float B, float A);
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
    LevelVector StartLocationOffset,
    LevelVector Acceleration,
    float ParticlesPerSecond,
    bool SpinParticles,
    ParticleNumberRange Spin,
    LevelVector SpinDirection,
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
    LevelVector Location,
    LevelRotation Rotation,
    float Duration,
    string? Target,
    IReadOnlyDictionary<string, string> Properties,
    string? Owner = null,
    string? ResourceUrl = null,
    ParticleEmitterManifestEntry? Particle = null,
    string? Diagnostic = null);

internal sealed record LevelTerrainManifestEntry(
    string Name,
    LevelVector Location,
    LevelRotation Rotation,
    LevelVector Scale,
    string? Heightmap,
    int HeightmapWidth,
    int HeightmapHeight,
    string? MeshUrl,
    IReadOnlyList<LevelTerrainLayerManifestEntry> Layers,
    IReadOnlyList<string> ControlMapUrls,
    int ControlMapWidth,
    int ControlMapHeight,
    string ControlMapEncoding,
    int ControlMapArrayGroup,
    string MaterialStatus,
    string? MaterialError);

internal sealed record LevelTerrainLayerManifestEntry(
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
    LevelRotation LayerRotation,
    LevelTerrainUvTransform UvTransform);

internal sealed record LevelTerrainUvTransform(
    LevelTerrainUvTransformRow U,
    LevelTerrainUvTransformRow V);

internal sealed record LevelTerrainUvTransformRow(
    float X,
    float Y,
    float Z,
    float Offset);

internal sealed record LevelActorManifestEntry(
    string Name,
    string ClassName,
    LevelVector Location,
    LevelRotation Rotation,
    LevelVector PrePivot,
    float DrawScale,
    LevelVector DrawScale3D,
    string? MeshPackage,
    string? MeshObject,
    string? MeshUrl,
    LevelVertexLightingReference? VertexLighting);

internal sealed record LevelVertexLightingReference(
    string Url,
    int TextureWidth,
    int TextureHeight,
    int TexelOffset,
    int VertexCount);

internal sealed record LevelEnvironmentManifestEntry(
    LevelColor AmbientColor,
    float AmbientBrightness,
    LevelDistanceFog? DistanceFog);

internal sealed record LevelDistanceFog(
    LevelColor Color,
    float Start,
    float End);

internal sealed record LevelColor(float R, float G, float B);

internal sealed record LevelLightManifestEntry(
    string Name,
    string ClassName,
    LevelVector Location,
    LevelRotation Rotation,
    float Brightness,
    byte Hue,
    byte Saturation,
    float Radius,
    IReadOnlyDictionary<string, string>? Properties = null,
    string? ResourceUrl = null);

internal sealed record LevelWaterVolumeManifestEntry(
    string Name,
    string ClassName,
    string? BrushName,
    LevelVector Location,
    LevelRotation Rotation,
    LevelVector PrePivot,
    float DrawScale,
    LevelVector DrawScale3D,
    string? MeshUrl,
    int VertexCount,
    int TriangleCount,
    string Status,
    string? Error);

internal sealed record LevelBspMeshManifestEntry(
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

internal sealed record LevelVector(float X, float Y, float Z);
internal sealed record LevelRotation(int Pitch, int Yaw, int Roll);
