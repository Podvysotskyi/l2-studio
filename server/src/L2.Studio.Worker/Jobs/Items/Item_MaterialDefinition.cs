namespace L2.Studio.Worker;

public sealed record Item_MaterialDefinition : ItemDefinition
{
    public bool? ImmediateEffect { get; init; }
    public bool? IsStackable { get; init; }
}
