using L2.Studio.Context.Identifiers;

namespace L2.Studio.Worker;

public sealed record SkillDefinition(
    int Id,
    short Levels,
    string Name,
    SkillOperateTypeId? OperateTypeId,
    SkillTargetTypeId? TargetTypeId,
    IReadOnlyList<SkillIconDefinition> Icons);
