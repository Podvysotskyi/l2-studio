namespace L2.Studio.Content.Entities;

public sealed class SkillIcon
{
    public int SkillId { get; set; }
    public short Level { get; set; }
    public required string Name { get; set; }
    public Skill Skill { get; set; } = null!;
}
