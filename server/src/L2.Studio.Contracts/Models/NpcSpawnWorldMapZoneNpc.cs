namespace L2.Studio.Contracts;

public sealed record NpcSpawnWorldMapZoneNpc(
    int NpcId,
    string? NpcName,
    int Count,
    int RespawnDelaySeconds,
    int? RespawnRandomSeconds);
