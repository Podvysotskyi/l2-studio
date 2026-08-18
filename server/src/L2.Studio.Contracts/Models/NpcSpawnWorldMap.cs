namespace L2.Studio.Contracts;

public sealed record NpcSpawnWorldMap(
    IReadOnlyList<NpcSpawnWorldMapZone> Zones,
    IReadOnlyList<NpcSpawnWorldMapPoint> Points);
