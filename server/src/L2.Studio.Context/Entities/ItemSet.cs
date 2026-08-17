using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_sets")]
[PrimaryKey(nameof(GameVersion), nameof(SetId))]
public sealed class ItemSet
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("set_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int SetId { get; set; }
    public ICollection<ItemSetBodyPart> BodyParts { get; } = [];
    public ICollection<ItemSetSkill> Skills { get; } = [];
    public ItemSetStats? Stats { get; set; }
}
