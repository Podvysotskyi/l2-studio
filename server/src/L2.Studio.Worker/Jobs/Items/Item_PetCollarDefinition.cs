namespace L2.Studio.Worker;

public sealed record Item_PetCollarDefinition : ItemDefinition, IItemSkillsDefinition
{
    public string? ActionName { get; init; }
    public string? HandlerName { get; init; }
    public string? UseCondition { get; init; }
    public bool? IsOlyRestricted { get; init; }
    public IReadOnlyList<ItemSkillDefinition> Skills { get; init; } = [];
}
