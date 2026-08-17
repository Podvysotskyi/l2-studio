using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_conditions")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class ItemCondition
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int ItemId { get; set; }
    [Column("message_id")] public int MessageId { get; set; }
    [Column("add_name")] public bool AddName { get; set; }
    public Item Item { get; set; } = null!;
    public ItemCondition_Player Player { get; set; } = null!;
}
