namespace L2.Studio.Worker;

public sealed record Item_ArrowDefinition : ItemDefinition
{
    public string? ActionName { get; init; }
    public string? BodyPartName { get; init; }
    public string? CrystalTypeName { get; init; }
    public bool? ImmediateEffect { get; init; }
    public bool? IsStackable { get; init; }
}
