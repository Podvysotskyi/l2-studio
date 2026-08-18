namespace L2.Studio.Worker;

public sealed record NpcSpawnEntityDefinition(
    int Sequence,
    int NpcId,
    int X,
    int Y,
    int Z,
    int Heading,
    int RespawnDelaySeconds);
