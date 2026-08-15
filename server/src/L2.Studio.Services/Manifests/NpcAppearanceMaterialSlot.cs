namespace L2.Studio.Services;

internal sealed record NpcAppearanceMaterialSlot(
    int SectionIndex,
    NpcMaterialReference? DefaultMaterial,
    NpcMaterialReference? OverrideMaterial,
    NpcMaterialReference? EffectiveMaterial,
    string EffectiveSource,
    string? Warning);
