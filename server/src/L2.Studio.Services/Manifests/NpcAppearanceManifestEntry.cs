namespace L2.Studio.Services;

internal sealed record NpcAppearanceManifestEntry(
    int Id,
    uint AppearanceId,
    string AppearanceName,
    float Speed,
    string ClassName,
    NpcAnimationAssetReference Mesh,
    IReadOnlyList<NpcMaterialReference> Textures,
    IReadOnlyList<NpcAppearanceMaterialSlot> MaterialSlots,
    float CollisionRadius,
    float CollisionHeight,
    IReadOnlyList<NpcAssetReference> AttackSounds,
    IReadOnlyList<NpcAssetReference> DefenceSounds,
    IReadOnlyList<NpcAssetReference> DamageSounds,
    float SoundVolume,
    float SoundRadius,
    float SoundRandomness,
    NpcAssetReference AttackEffect);
