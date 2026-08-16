namespace L2.Studio.Contracts;

public sealed record ItemPropertiesSummary(
    int? DisplayId,
    int? CrystalCount,
    int? Soulshots,
    int? Spiritshots,
    int? MpConsume,
    string? ReducedMpConsume,
    int? ReuseDelay,
    int? RecipeId,
    string? ItemSkill,
    string? UseCondition,
    bool? ElementEnabled,
    bool? IsAttackWeapon,
    bool? IsForceEquip,
    bool? IsMagicWeapon,
    bool? IsQuestItem,
    bool? UseWeaponSkillsOnly);
