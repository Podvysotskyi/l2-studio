using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_set_skills")]
[PrimaryKey(nameof(GameVersion), nameof(SetId), nameof(SkillId), nameof(SkillLevel))]
public sealed class ItemSetSkill
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("set_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int SetId { get; set; }
    [Column("skill_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int SkillId { get; set; }
    [Column("skill_level"), DatabaseGenerated(DatabaseGeneratedOption.None)] public short SkillLevel { get; set; }
    public ItemSet ItemSet { get; set; } = null!;
    public Skill Skill { get; set; } = null!;
}
