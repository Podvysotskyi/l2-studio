namespace L2.Studio.Worker;

public sealed record Item_ArmorDefinition : ItemDefinition, IItemStatsDefinition
{
    public string? ActionName { get; init; }
    public string? BodyPartName { get; init; }
    public string? CrystalTypeName { get; init; }
    public int? CrystalCount { get; init; }
    public bool? EnchantEnabled { get; init; }
    public bool? ForNpc { get; init; }
    public bool? ImmediateEffect { get; init; }
    public bool? IsDepositable { get; init; }
    public bool? IsDestroyable { get; init; }
    public bool? IsDropable { get; init; }
    public bool? IsSellable { get; init; }
    public bool? IsTradable { get; init; }
    public ItemStatsDefinition? Stats { get; init; }
}
