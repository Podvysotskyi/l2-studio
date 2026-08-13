namespace L2.Studio.Contracts;

public sealed record NpcSummary(
    int Id,
    short Level,
    string? Name,
    string NpcTypeName,
    string NpcTypeDisplayName,
    string? NpcRaceName,
    string? NpcRaceDisplayName,
    string NpcSexName,
    string NpcSexDisplayName);
