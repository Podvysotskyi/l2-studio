using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_skills")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId), nameof(SkillId), nameof(SkillLevel))]
public sealed class ItemSkill
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemId { get; set; }
    [Column("skill_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int SkillId { get; set; }
    [Column("skill_level"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public short SkillLevel { get; set; }
    [Column("item_skill_type_name"), MaxLength(64)]
    public string? ItemSkillTypeName { get; set; }
    [Column("chance")]
    public int? Chance { get; set; }
    public Item Item { get; set; } = null!;
    public ItemSkillType? ItemSkillType { get; set; }
}
