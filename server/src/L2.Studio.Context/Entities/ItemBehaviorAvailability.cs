using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_behavior_availability")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class ItemBehaviorAvailability
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int ItemId { get; set; }
    [Column("enchant_enabled")] public bool? EnchantEnabled { get; set; }
    [Column("for_npc")] public bool? ForNpc { get; set; }
    [Column("immediate_effect")] public bool? ImmediateEffect { get; set; }
    [Column("is_depositable")] public bool? IsDepositable { get; set; }
    [Column("is_destroyable")] public bool? IsDestroyable { get; set; }
    [Column("is_dropable")] public bool? IsDropable { get; set; }
    [Column("is_oly_restricted")] public bool? IsOlyRestricted { get; set; }
    [Column("is_sellable")] public bool? IsSellable { get; set; }
    [Column("is_stackable")] public bool? IsStackable { get; set; }
    [Column("is_tradable")] public bool? IsTradable { get; set; }
    public Item Item { get; set; } = null!;
}
