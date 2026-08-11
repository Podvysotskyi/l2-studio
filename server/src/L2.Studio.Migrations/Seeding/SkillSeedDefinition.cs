using L2.Studio.Context.Identifiers;

namespace L2.Studio.Migrations.Seeding;

public sealed record SkillSeedDefinition(
    int Id,
    short Levels,
    string Name,
    SkillOperateTypeId? SkillOperateTypeId,
    SkillTargetTypeId? SkillTargetTypeId);
