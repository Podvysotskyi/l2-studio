namespace L2.Studio.Contracts;

public sealed record ItemSetBodyPartSummary(
    string BodyPartName,
    string BodyPartDisplayName,
    int ItemId,
    string? ItemName);
