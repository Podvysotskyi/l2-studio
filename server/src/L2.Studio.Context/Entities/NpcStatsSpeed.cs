using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_stats_speed")]
[PrimaryKey(nameof(GameVersion), nameof(NpcId))]
public sealed class NpcStatsSpeed : INpcStatsRecord
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NpcId { get; set; }
    [Column("walk_ground")]
    public decimal? WalkGround { get; set; }
    [Column("run_ground")]
    public decimal? RunGround { get; set; }
    public Npc Npc { get; set; } = null!;
}
