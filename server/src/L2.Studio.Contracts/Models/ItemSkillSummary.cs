namespace L2.Studio.Contracts;

public sealed record ItemSkillSummary(
    int SkillId,
    short SkillLevel,
    string? SkillName,
    string? ItemSkillTypeName,
    string? ItemSkillTypeDisplayName,
    int? Chance);
