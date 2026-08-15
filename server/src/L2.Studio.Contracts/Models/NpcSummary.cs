namespace L2.Studio.Contracts;

public sealed record NpcSummary(
    int Id,
    int? AppearanceId,
    short Level,
    string? Name,
    string NpcTypeName,
    string NpcTypeDisplayName,
    string? NpcRaceName,
    string? NpcRaceDisplayName,
    string NpcSexName,
    string NpcSexDisplayName,
    bool HasVisuals,
    NpcStatusSummary? Status = null,
    NpcStatsSummary? Stats = null,
    NpcStatsVitalsSummary? StatsVitals = null,
    NpcStatsAttackSummary? StatsAttack = null,
    NpcStatsDefenceSummary? StatsDefence = null,
    NpcStatsSpeedSummary? StatsSpeed = null);
