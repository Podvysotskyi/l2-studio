using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_actions")]
[PrimaryKey(nameof(GameVersion), nameof(Name))]
public sealed class ItemAction
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("name"), MaxLength(64)]
    public required string Name { get; set; }
    [Column("display_name"), MaxLength(64)]
    public required string DisplayName { get; set; }
    public ICollection<Item> Items { get; } = [];
}
