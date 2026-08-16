namespace L2.Studio.Contracts.Requests;

public sealed record CreateItemSkillRequest(
    int SkillId,
    short SkillLevel,
    string? ItemSkillTypeName,
    int? Chance);
