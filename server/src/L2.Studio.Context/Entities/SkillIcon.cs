using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("skill_icons")]
[PrimaryKey(nameof(GameVersion), nameof(SkillId), nameof(Level))]
public sealed class SkillIcon
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("skill_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int SkillId { get; set; }
    [Column("level"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public short Level { get; set; }
    [Column("name"), MaxLength(64)]
    public required string Name { get; set; }
    public Skill Skill { get; set; } = null!;
}
