using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_stats_vitals")]
[PrimaryKey(nameof(GameVersion), nameof(NpcId))]
public sealed class NpcStatsVitals : INpcStatsRecord
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NpcId { get; set; }
    [Column("hp")]
    public decimal? Hp { get; set; }
    [Column("hp_regen")]
    public decimal? HpRegen { get; set; }
    [Column("mp")]
    public decimal? Mp { get; set; }
    [Column("mp_regen")]
    public decimal? MpRegen { get; set; }
    public Npc Npc { get; set; } = null!;
}
