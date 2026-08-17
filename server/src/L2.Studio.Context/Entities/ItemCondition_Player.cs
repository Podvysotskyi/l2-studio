using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_condition_players")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class ItemCondition_Player
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int ItemId { get; set; }
    [Column("is_pvp_flagged")] public bool? IsPvpFlagged { get; set; }
    [Column("player_races"), MaxLength(128)] public string? PlayerRaces { get; set; }
    [Column("player_category_types"), MaxLength(128)] public string? PlayerCategoryTypes { get; set; }
    public ItemCondition Condition { get; set; } = null!;
}
