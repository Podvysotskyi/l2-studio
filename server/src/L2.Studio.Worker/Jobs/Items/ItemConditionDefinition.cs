namespace L2.Studio.Worker;

public sealed record ItemConditionDefinition(
    int MessageId,
    bool AddName,
    bool? IsPvpFlagged,
    string? PlayerRaces,
    string? PlayerCategoryTypes);
