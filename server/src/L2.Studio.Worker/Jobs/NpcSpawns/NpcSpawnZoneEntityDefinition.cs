namespace L2.Studio.Worker;

public sealed record NpcSpawnZoneEntityDefinition(
    int Sequence,
    int NpcId,
    int Count,
    int RespawnDelaySeconds,
    int? RespawnRandomSeconds);
