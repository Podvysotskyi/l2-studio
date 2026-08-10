using L2.Studio.Content.Identifiers;

namespace L2.Studio.Content.Entities;

public sealed class SkillOperateType
{
    public SkillOperateTypeId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Skill> Skills { get; set; } = [];
}
