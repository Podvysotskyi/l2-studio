namespace L2.Studio.Contracts.Requests;

public sealed record UpdateSkillRequest(
    string? Name,
    short Levels,
    string? SkillOperateTypeName,
    string? SkillTargetTypeName);
