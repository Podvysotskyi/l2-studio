namespace L2.Studio.Contracts;

public sealed record SkillSummary(
    int Id,
    short Levels,
    string Name,
    string? SkillOperateTypeName,
    string? SkillOperateTypeDisplayName,
    string? SkillTargetTypeName,
    string? SkillTargetTypeDisplayName,
    int IconCount);
