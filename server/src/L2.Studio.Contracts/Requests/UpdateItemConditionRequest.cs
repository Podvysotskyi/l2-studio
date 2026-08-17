namespace L2.Studio.Contracts.Requests;

public sealed record UpdateItemConditionRequest(
    int MessageId,
    bool AddName,
    bool? IsPvpFlagged,
    IReadOnlyList<string>? PlayerRaces,
    IReadOnlyList<string>? PlayerCategoryTypes);
