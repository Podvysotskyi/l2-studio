using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_armor")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class Item_Armor
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int ItemId { get; set; }
    [Column("item_action_name"), MaxLength(64)] public string? ItemActionName { get; set; }
    [Column("item_body_part_name"), MaxLength(64)] public string? ItemBodyPartName { get; set; }
    [Column("item_crystal_type_name"), MaxLength(64)] public string? ItemCrystalTypeName { get; set; }
    [Column("crystal_count")] public int? CrystalCount { get; set; }
    public Item Item { get; set; } = null!;
    public ItemAction? ItemAction { get; set; }
    public ItemBodyPart? ItemBodyPart { get; set; }
    public ItemCrystalType? ItemCrystalType { get; set; }
}
