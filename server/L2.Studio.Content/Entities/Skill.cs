using L2.Studio.Content.Identifiers;

namespace L2.Studio.Content.Entities;

public sealed class Skill
{
    public int Id { get; set; }
    public short Levels { get; set; }
    public required string Name { get; set; }
    public SkillOperateTypeId? SkillOperateTypeId { get; set; }
    public SkillTargetTypeId? SkillTargetTypeId { get; set; }
    public SkillOperateType? SkillOperateType { get; set; }
    public SkillTargetType? SkillTargetType { get; set; }
    public ICollection<SkillIcon> SkillIcons { get; set; } = [];
}
