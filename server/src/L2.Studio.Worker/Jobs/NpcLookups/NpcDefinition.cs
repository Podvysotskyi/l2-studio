namespace L2.Studio.Worker;

public sealed record NpcDefinition(
    int Id,
    int AppearanceId,
    short Level,
    string? Name,
    string TypeName,
    string? RaceName,
    string SexName,
    NpcStatusDefinition? Status,
    NpcStatsDefinition? Stats = null,
    NpcStatsVitalsDefinition? StatsVitals = null,
    NpcStatsAttackDefinition? StatsAttack = null,
    NpcStatsDefenceDefinition? StatsDefence = null,
    NpcStatsSpeedDefinition? StatsSpeed = null);
