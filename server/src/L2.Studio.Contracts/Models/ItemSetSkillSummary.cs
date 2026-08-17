namespace L2.Studio.Contracts;

public sealed record ItemSetSkillSummary(
    int SkillId,
    short SkillLevel,
    string? SkillName,
    short? SkillLevels);
