using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_stats_defence")]
[PrimaryKey(nameof(GameVersion), nameof(NpcId))]
public sealed class NpcStatsDefence : INpcStatsRecord
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NpcId { get; set; }
    [Column("physical")]
    public decimal? Physical { get; set; }
    [Column("magical")]
    public decimal? Magical { get; set; }
    [Column("evasion")]
    public int? Evasion { get; set; }
    [Column("shield")]
    public int? Shield { get; set; }
    [Column("shield_rate")]
    public int? ShieldRate { get; set; }
    public Npc Npc { get; set; } = null!;
}
