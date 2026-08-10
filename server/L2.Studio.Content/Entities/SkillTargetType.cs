using L2.Studio.Content.Identifiers;

namespace L2.Studio.Content.Entities;

public sealed class SkillTargetType
{
    public SkillTargetTypeId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Skill> Skills { get; set; } = [];
}
