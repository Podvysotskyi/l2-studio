namespace L2.Studio.Contracts;

public sealed record NpcSpawnWorldMapZone(
    string Name,
    short MinZ,
    short MaxZ,
    IReadOnlyList<NpcSpawnWorldMapTerritoryNode> TerritoryNodes,
    IReadOnlyList<NpcSpawnWorldMapZoneNpc> Npcs);
