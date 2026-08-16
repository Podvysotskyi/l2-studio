namespace L2.Studio.Contracts;

public sealed record ItemPrimarySkillSummary(
    string Value,
    int? SkillId,
    short? SkillLevel,
    string? SkillName);
