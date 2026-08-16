namespace L2.Studio.Worker;

public abstract record ItemDefinition
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public required string TypeName { get; init; }
    public string? MaterialName { get; init; }
    public string? Icon { get; init; }
    public int? Weight { get; init; }
    public long? Price { get; init; }
}
