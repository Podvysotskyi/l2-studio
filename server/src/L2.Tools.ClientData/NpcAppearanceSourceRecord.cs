namespace L2.Tools.ClientData;

public sealed record NpcAppearanceSourceRecord(
    uint Id,
    string Name,
    float Speed,
    string ClassName,
    string Mesh,
    IReadOnlyList<string> Textures,
    float CollisionRadius,
    float CollisionHeight,
    IReadOnlyList<string> AttackSounds,
    IReadOnlyList<string> DefenceSounds,
    IReadOnlyList<string> DamageSounds,
    float SoundVolume,
    float SoundRadius,
    float SoundRandomness,
    string AttackEffect);
