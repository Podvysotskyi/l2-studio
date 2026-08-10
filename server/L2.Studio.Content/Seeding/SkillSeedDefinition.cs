using L2.Studio.Content.Identifiers;

namespace L2.Studio.Content.Seeding;

public sealed record SkillSeedDefinition(
    int Id,
    short Levels,
    string Name,
    SkillOperateTypeId? SkillOperateTypeId,
    SkillTargetTypeId? SkillTargetTypeId);
