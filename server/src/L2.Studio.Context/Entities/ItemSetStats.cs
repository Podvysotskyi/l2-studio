using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_set_stats")]
[PrimaryKey(nameof(GameVersion), nameof(SetId))]
public sealed class ItemSetStats
{
    [Column("game_version"), MaxLength(32)] public required string GameVersion { get; set; }
    [Column("set_id"), DatabaseGenerated(DatabaseGeneratedOption.None)] public int SetId { get; set; }
    [Column("str")] public int? Str { get; set; }
    [Column("dex")] public int? Dex { get; set; }
    [Column("con")] public int? Con { get; set; }
    [Column("int")] public int? Int { get; set; }
    [Column("wit")] public int? Wit { get; set; }
    [Column("men")] public int? Men { get; set; }
    public ItemSet ItemSet { get; set; } = null!;
}
