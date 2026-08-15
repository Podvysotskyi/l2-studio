namespace L2.Studio.Worker;

public sealed record SkillDefinition(
    int Id,
    short Levels,
    string Name,
    string? OperateTypeName,
    string? TargetTypeName,
    IReadOnlyList<SkillIconDefinition> Icons);
