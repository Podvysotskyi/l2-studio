using System.Numerics;

namespace L2.Tools.PackageReader;

public sealed record UnrealStaticMesh(
    string Name,
    IReadOnlyList<Vector3> Positions,
    IReadOnlyList<Vector3> Normals,
    IReadOnlyList<Vector2> TextureCoordinates,
    IReadOnlyList<ushort> Indices,
    IReadOnlyList<UnrealStaticMeshSection> Sections,
    IReadOnlyList<UnrealColor>? ColorStream0 = null,
    IReadOnlyList<UnrealColor>? ColorStream1 = null)
{
    public IReadOnlyList<UnrealColor> VertexColors0 { get; } = ColorStream0 ?? [];
    public IReadOnlyList<UnrealColor> VertexColors1 { get; } = ColorStream1 ?? [];
}

public sealed record UnrealStaticMeshSection(
    int FirstIndex,
    int IndexCount,
    UnrealObjectReference? Material = null);

public sealed record UnrealMaterialExport(
    string Name,
    string ClassName,
    UnrealObjectReference? Material,
    UnrealObjectReference? Diffuse,
    UnrealObjectReference? Opacity,
    UnrealObjectReference? SelfIllumination,
    byte OutputBlending,
    byte FrameBufferBlending,
    bool TwoSided,
    bool AlphaTest,
    byte AlphaRef,
    bool ZWrite,
    bool ZTest,
    UnrealObjectReference? Material2 = null,
    UnrealObjectReference? Mask = null,
    float PanRate = 0,
    float RotationRate = 0,
    byte CombineOperation = 0,
    byte AlphaOperation = 0,
    UnrealObjectReference? Detail = null,
    float DetailScale = 8,
    UnrealColor? ModifierColor = null,
    byte UOscillationType = 0,
    byte VOscillationType = 0,
    float UOscillationRate = 0,
    float VOscillationRate = 0,
    float UOscillationAmplitude = 0,
    float VOscillationAmplitude = 0,
    float UOscillationPhase = 0,
    float VOscillationPhase = 0,
    bool TreatAsTwoSided = false,
    UnrealObjectReference? SelfIlluminationMask = null,
    UnrealObjectReference? Specular = null,
    UnrealObjectReference? SpecularityMask = null,
    bool PerformLightingOnSpecularPass = false,
    UnrealColor? FadeColor1 = null,
    UnrealColor? FadeColor2 = null,
    byte ColorFadeType = 0,
    float FadePeriod = 0,
    float FadePhase = 0,
    bool InvertMask = false,
    bool Modulate2X = false,
    bool Modulate4X = false);
