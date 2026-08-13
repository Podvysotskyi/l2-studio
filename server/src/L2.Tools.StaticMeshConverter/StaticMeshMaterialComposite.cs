namespace L2.Tools.StaticMeshConverter;

public sealed record StaticMeshMaterialComposite(
    string? SecondaryUrl,
    StaticMeshMaterialTint? SecondaryTint,
    StaticMeshMaterialFade? SecondaryFade,
    string? MaskUrl,
    byte ColorOperation,
    byte AlphaOperation,
    bool InvertMask,
    float ModulateScale);
