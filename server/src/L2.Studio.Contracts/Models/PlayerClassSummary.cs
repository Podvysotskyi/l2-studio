namespace L2.Studio.Contracts;

public sealed record PlayerClassSummary(
    int Id,
    string Name,
    int? ParentClassId,
    bool IsMage,
    IReadOnlyList<PlayerClassRaceSummary> AllowedRaces);
