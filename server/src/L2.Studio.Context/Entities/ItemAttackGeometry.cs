using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_attack_geometries")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class ItemAttackGeometry
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemId { get; set; }
    [Column("offset_x")]
    public int OffsetX { get; set; }
    [Column("offset_y")]
    public int OffsetY { get; set; }
    [Column("radius")]
    public int Radius { get; set; }
    [Column("length")]
    public int Length { get; set; }
    public Item Item { get; set; } = null!;
}
