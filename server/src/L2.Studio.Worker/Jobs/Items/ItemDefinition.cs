namespace L2.Studio.Worker;

public sealed record ItemDefinition
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public string? ActionName { get; init; }
    public string? BodyPartName { get; init; }
    public string? MaterialName { get; init; }
    public string? CrystalTypeName { get; init; }
    public string? Icon { get; init; }
    public string? WeaponType { get; init; }
    public string? ArmorType { get; init; }
    public string? EtcItemType { get; init; }
    public string? DamageRange { get; init; }
    public int? DisplayId { get; init; }
    public int? CrystalCount { get; init; }
    public int? Weight { get; init; }
    public long? Price { get; init; }
    public int? Soulshots { get; init; }
    public int? Spiritshots { get; init; }
    public int? MpConsume { get; init; }
    public string? ReducedMpConsume { get; init; }
    public int? ReuseDelay { get; init; }
    public int? RecipeId { get; init; }
    public string? Handler { get; init; }
    public string? ItemSkill { get; init; }
    public string? UseCondition { get; init; }
    public bool? ElementEnabled { get; init; }
    public bool? EnchantEnabled { get; init; }
    public bool? ForNpc { get; init; }
    public bool? ImmediateEffect { get; init; }
    public bool? IsAttackWeapon { get; init; }
    public bool? IsForceEquip { get; init; }
    public bool? IsDepositable { get; init; }
    public bool? IsDestroyable { get; init; }
    public bool? IsDropable { get; init; }
    public bool? IsMagicWeapon { get; init; }
    public bool? IsOlyRestricted { get; init; }
    public bool? IsQuestItem { get; init; }
    public bool? IsSellable { get; init; }
    public bool? IsStackable { get; init; }
    public bool? IsTradable { get; init; }
    public bool? UseWeaponSkillsOnly { get; init; }
    public ItemStatsDefinition? Stats { get; init; }
}
