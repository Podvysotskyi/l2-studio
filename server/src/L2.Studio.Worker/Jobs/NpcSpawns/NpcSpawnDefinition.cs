namespace L2.Studio.Worker;

public sealed record NpcSpawnDefinition(
    string Name,
    IReadOnlyList<NpcSpawnEntityDefinition> Entities);
