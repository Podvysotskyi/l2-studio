namespace L2.Studio.Context.Entities;

public sealed class SkillIcon
{
    public string GameVersion { get; set; } = "interlude";
    public int SkillId { get; set; }
    public short Level { get; set; }
    public required string Name { get; set; }
    public Skill Skill { get; set; } = null!;
}
