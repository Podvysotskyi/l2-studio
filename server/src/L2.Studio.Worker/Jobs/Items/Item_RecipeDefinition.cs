namespace L2.Studio.Worker;

public sealed record Item_RecipeDefinition : ItemDefinition
{
    public string? ActionName { get; init; }
    public int? RecipeId { get; init; }
    public string? HandlerName { get; init; }
    public bool? ImmediateEffect { get; init; }
    public bool? IsDepositable { get; init; }
    public bool? IsDestroyable { get; init; }
    public bool? IsDropable { get; init; }
    public bool? IsSellable { get; init; }
    public bool? IsStackable { get; init; }
    public bool? IsTradable { get; init; }
}
