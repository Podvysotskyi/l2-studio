using L2.Studio.Context.Identifiers;

namespace L2.Studio.Context.Entities;

public sealed class SkillOperateType
{
    public required string GameVersion { get; set; }
    public SkillOperateTypeId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Skill> Skills { get; set; } = [];
}
