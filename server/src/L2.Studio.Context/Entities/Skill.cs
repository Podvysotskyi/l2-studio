using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("skills")]
[PrimaryKey(nameof(GameVersion), nameof(Id))]
public sealed class Skill
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    [Column("levels")]
    public short Levels { get; set; }
    [Column("name"), MaxLength(100)]
    public required string Name { get; set; }
    [Column("skill_operate_type_name"), MaxLength(64)]
    public string? SkillOperateTypeName { get; set; }
    [Column("skill_target_type_name"), MaxLength(64)]
    public string? SkillTargetTypeName { get; set; }
    public SkillOperateType? SkillOperateType { get; set; }
    public SkillTargetType? SkillTargetType { get; set; }
    public ICollection<SkillIcon> SkillIcons { get; set; } = [];
}
