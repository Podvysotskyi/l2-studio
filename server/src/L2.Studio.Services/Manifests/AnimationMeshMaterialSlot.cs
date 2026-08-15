namespace L2.Studio.Services;

internal sealed record AnimationMeshMaterialSlot(
    int SectionIndex,
    TextureMaterialReference? Reference,
    string Status);
