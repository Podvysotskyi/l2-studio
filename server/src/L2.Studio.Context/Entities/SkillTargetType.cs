using L2.Studio.Context.Identifiers;

namespace L2.Studio.Context.Entities;

public sealed class SkillTargetType
{
    public string GameVersion { get; set; } = "interlude";
    public SkillTargetTypeId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Skill> Skills { get; set; } = [];
}
