namespace L2.Studio.Worker;

public sealed record Item_EtcDefinition : ItemDefinition, IItemSkillsDefinition, IItemStatsDefinition
{
    public string? ActionName { get; init; }
    public string? BodyPartName { get; init; }
    public string? CrystalTypeName { get; init; }
    public int? DisplayId { get; init; }
    public int? ReuseDelay { get; init; }
    public string? HandlerName { get; init; }
    public string? ItemSkill { get; init; }
    public string? UseCondition { get; init; }
    public bool? ForNpc { get; init; }
    public bool? ImmediateEffect { get; init; }
    public bool? IsDepositable { get; init; }
    public bool? IsDestroyable { get; init; }
    public bool? IsDropable { get; init; }
    public bool? IsOlyRestricted { get; init; }
    public bool? IsQuestItem { get; init; }
    public bool? IsSellable { get; init; }
    public bool? IsStackable { get; init; }
    public bool? IsTradable { get; init; }
    public IReadOnlyList<ItemSkillDefinition> Skills { get; init; } = [];
    public ItemStatsDefinition? Stats { get; init; }
}
