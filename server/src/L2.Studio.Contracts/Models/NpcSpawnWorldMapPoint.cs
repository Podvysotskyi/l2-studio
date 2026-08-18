namespace L2.Studio.Contracts;

public sealed record NpcSpawnWorldMapPoint(
    string SpawnName,
    int Sequence,
    int NpcId,
    string? NpcName,
    int X,
    int Y,
    int Z,
    int Heading,
    int RespawnDelaySeconds);
