using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_etc")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class Item_Etc
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int ItemId { get; set; }
    [Column("item_action_name"), MaxLength(64)] public string? ItemActionName { get; set; }
    [Column("item_body_part_name"), MaxLength(64)] public string? ItemBodyPartName { get; set; }
    [Column("item_crystal_type_name"), MaxLength(64)] public string? ItemCrystalTypeName { get; set; }
    [Column("display_id")] public int? DisplayId { get; set; }
    [Column("reuse_delay")] public int? ReuseDelay { get; set; }
    [Column("handler"), MaxLength(64)] public string? HandlerName { get; set; }
    [Column("item_skill"), MaxLength(64)] public string? ItemSkill { get; set; }
    [Column("use_condition"), MaxLength(512)] public string? UseCondition { get; set; }
    [Column("is_questitem")] public bool? IsQuestItem { get; set; }
    public Item Item { get; set; } = null!;
    public ItemAction? ItemAction { get; set; }
    public ItemBodyPart? ItemBodyPart { get; set; }
    public ItemCrystalType? ItemCrystalType { get; set; }
    public ItemHandler? ItemHandler { get; set; }
}
