using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_stats")]
[PrimaryKey(nameof(GameVersion), nameof(NpcId))]
public sealed class NpcStats : INpcStatsRecord
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NpcId { get; set; }
    [Column("str")]
    public int? Str { get; set; }
    [Column("int")]
    public int? Int { get; set; }
    [Column("dex")]
    public int? Dex { get; set; }
    [Column("wit")]
    public int? Wit { get; set; }
    [Column("con")]
    public int? Con { get; set; }
    [Column("men")]
    public int? Men { get; set; }
    [Column("hit_time")]
    public int? HitTime { get; set; }
    public Npc Npc { get; set; } = null!;
}
