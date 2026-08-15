namespace L2.Studio.Contracts;

public sealed record PlayerAppearanceSummary(
    int Id,
    string Name,
    int PlayerRaceId,
    string PlayerRaceName,
    int PlayerSexId,
    string PlayerSexName);
