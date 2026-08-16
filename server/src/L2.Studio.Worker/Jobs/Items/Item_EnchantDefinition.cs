namespace L2.Studio.Worker;

public sealed record Item_EnchantDefinition : ItemDefinition, IItemSkillsDefinition
{
    public string? ActionName { get; init; }
    public string? HandlerName { get; init; }
    public bool? ImmediateEffect { get; init; }
    public bool? IsOlyRestricted { get; init; }
    public bool? IsStackable { get; init; }
    public IReadOnlyList<ItemSkillDefinition> Skills { get; init; } = [];
}
