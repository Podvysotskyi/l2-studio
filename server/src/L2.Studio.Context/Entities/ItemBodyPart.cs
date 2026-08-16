using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_body_parts")]
[PrimaryKey(nameof(GameVersion), nameof(Name))]
public sealed class ItemBodyPart
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("name"), MaxLength(64)]
    public required string Name { get; set; }
    [Column("display_name"), MaxLength(64)]
    public required string DisplayName { get; set; }
}
