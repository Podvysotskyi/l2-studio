namespace L2.Studio.Worker;

public sealed record Item_WeaponDefinition : ItemDefinition, IItemSkillsDefinition, IItemStatsDefinition
{
    public string? ActionName { get; init; }
    public string? BodyPartName { get; init; }
    public string? CrystalTypeName { get; init; }
    public int? DisplayId { get; init; }
    public int? CrystalCount { get; init; }
    public int? Soulshots { get; init; }
    public int? Spiritshots { get; init; }
    public int? MpConsume { get; init; }
    public string? ReducedMpConsume { get; init; }
    public int? ReuseDelay { get; init; }
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
    public bool? IsSellable { get; init; }
    public bool? IsTradable { get; init; }
    public bool? UseWeaponSkillsOnly { get; init; }
    public ItemAttackGeometryDefinition? AttackGeometry { get; init; }
    public IReadOnlyList<ItemSkillDefinition> Skills { get; init; } = [];
    public ItemStatsDefinition? Stats { get; init; }
}
