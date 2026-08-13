namespace L2.Studio.Contracts;

public sealed record SkillSummary(
    int Id,
    short Levels,
    string Name,
    int? SkillOperateTypeId,
    string? SkillOperateType,
    int? SkillTargetTypeId,
    string? SkillTargetType,
    int IconCount);
