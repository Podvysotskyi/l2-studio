using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_material")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class Item_Material
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
}
