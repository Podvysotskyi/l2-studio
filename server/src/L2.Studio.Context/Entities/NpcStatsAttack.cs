using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_stats_attack")]
[PrimaryKey(nameof(GameVersion), nameof(NpcId))]
public sealed class NpcStatsAttack : INpcStatsRecord
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NpcId { get; set; }
    [Column("physical")]
    public decimal? Physical { get; set; }
    [Column("magical")]
    public decimal? Magical { get; set; }
    [Column("random")]
    public int? Random { get; set; }
    [Column("critical")]
    public int? Critical { get; set; }
    [Column("accuracy")]
    public decimal? Accuracy { get; set; }
    [Column("attack_speed")]
    public int? AttackSpeed { get; set; }
    [Column("reuse_delay")]
    public int? ReuseDelay { get; set; }
    [Column("type"), MaxLength(16)]
    public string? Type { get; set; }
    [Column("range")]
    public int? Range { get; set; }
    [Column("distance")]
    public int? Distance { get; set; }
    [Column("width")]
    public int? Width { get; set; }
    public Npc Npc { get; set; } = null!;
}
