namespace L2.Studio.Worker;

public sealed record PlayerLookupDefinition<TId>(TId Id, string Name) where TId : struct, Enum;
