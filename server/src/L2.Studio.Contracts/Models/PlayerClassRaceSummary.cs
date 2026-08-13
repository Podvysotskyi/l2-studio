namespace L2.Studio.Contracts;

public sealed record PlayerClassRaceSummary(
    int Id,
    string Name,
    IReadOnlyList<PlayerSexSummary> AllowedSexes);
