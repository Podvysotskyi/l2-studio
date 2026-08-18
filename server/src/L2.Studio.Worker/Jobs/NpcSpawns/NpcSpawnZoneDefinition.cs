namespace L2.Studio.Worker;

public sealed record NpcSpawnZoneDefinition(
    string Name,
    short MinZ,
    short MaxZ,
    IReadOnlyList<NpcSpawnZoneTerritoryNodeDefinition> TerritoryNodes,
    IReadOnlyList<NpcSpawnZoneEntityDefinition> Entities);
