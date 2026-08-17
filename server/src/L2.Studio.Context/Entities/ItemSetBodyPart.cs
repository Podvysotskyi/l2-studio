using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_set_body_parts")]
[PrimaryKey(nameof(GameVersion), nameof(SetId), nameof(BodyPartName))]
public sealed class ItemSetBodyPart
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("set_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int SetId { get; set; }
    [Column("body_part_name"), MaxLength(64)] public required string BodyPartName { get; set; }
    [Column("item_id")] public int ItemId { get; set; }
    public ItemSet ItemSet { get; set; } = null!;
    public ItemBodyPart BodyPart { get; set; } = null!;
}
