namespace L2.Studio.Services;

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
    int MaterialCount,
    string OriginalFolder = "",
    string Path = "");

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
    TextureAnimationManifestEntry? Animation = null,
    string OriginalFolder = "",
    string Path = "");

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
