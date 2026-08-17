namespace L2.Studio.Contracts;

public sealed record ItemConditionSummary(
    int MessageId,
    bool AddName,
    bool? IsPvpFlagged,
    string? PlayerRaces,
    string? PlayerCategoryTypes);
