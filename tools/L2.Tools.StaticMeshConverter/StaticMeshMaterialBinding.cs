namespace L2.Tools.StaticMeshConverter;

public enum StaticMeshBlendMode
{
    Opaque,
    Masked,
    AlphaBlend,
    Additive,
    Modulate,
    Invisible
}

public enum StaticMeshOpacitySource
{
    None,
    Texture
}

public enum StaticMeshOpacityChannel
{
    Alpha,
    Luminance
}

public sealed record StaticMeshTextureAnimation(
    IReadOnlyList<string> FrameUrls,
    float FrameRate);

public sealed record StaticMeshMaterialTint(float R, float G, float B, float A);

public sealed record StaticMeshMaterialFade(
    StaticMeshMaterialTint Color1,
    StaticMeshMaterialTint Color2,
    byte Type,
    float Period,
    float Phase);

public sealed record StaticMeshMaterialComposite(
    string? SecondaryUrl,
    StaticMeshMaterialTint? SecondaryTint,
    StaticMeshMaterialFade? SecondaryFade,
    string? MaskUrl,
    byte ColorOperation,
    byte AlphaOperation,
    bool InvertMask,
    float ModulateScale);

public sealed record StaticMeshUvOscillation(
    byte UType,
    byte VType,
    float URate,
    float VRate,
    float UAmplitude,
    float VAmplitude,
    float UPhase,
    float VPhase);

public enum StaticMeshWindMode
{
    None,
    Grass,
    Foliage
}

public sealed record StaticMeshMaterialBinding(
    string Name,
    string? DiffuseUrl,
    string? OpacityUrl,
    string? EmissiveUrl,
    StaticMeshBlendMode BlendMode,
    bool DoubleSided,
    float AlphaCutoff,
    bool DepthWrite,
    bool DepthTest,
    StaticMeshOpacitySource OpacitySource = StaticMeshOpacitySource.None,
    StaticMeshOpacityChannel OpacityChannel = StaticMeshOpacityChannel.Alpha,
    float PanRate = 0,
    float PanRateV = 0,
    float RotationRate = 0,
    string? DetailUrl = null,
    float DetailScale = 8,
    StaticMeshTextureAnimation? DiffuseAnimation = null,
    StaticMeshTextureAnimation? OpacityAnimation = null,
    StaticMeshTextureAnimation? EmissiveAnimation = null,
    StaticMeshWindMode WindMode = StaticMeshWindMode.None,
    StaticMeshMaterialTint? Tint = null,
    StaticMeshUvOscillation? UvOscillation = null,
    bool Unlit = false,
    StaticMeshMaterialFade? Fade = null,
    StaticMeshMaterialComposite? Composite = null,
    string? SelfIlluminationMaskUrl = null,
    string? SpecularUrl = null,
    string? SpecularityMaskUrl = null,
    bool PerformLightingOnSpecularPass = false);
