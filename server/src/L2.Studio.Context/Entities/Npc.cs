using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npcs")]
[PrimaryKey(nameof(GameVersion), nameof(Id))]
public sealed class Npc
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    [Column("appearance_id")]
    public int? AppearanceId { get; set; }
    [Column("level")]
    public short Level { get; set; }
    [Column("name"), MaxLength(100)]
    public string? Name { get; set; }
    [Column("npc_type_name"), MaxLength(64)]
    public required string NpcTypeName { get; set; }
    [Column("npc_race_name"), MaxLength(64)]
    public string? NpcRaceName { get; set; }
    [Column("npc_sex_name"), MaxLength(64)]
    public required string NpcSexName { get; set; }
    public NpcType NpcType { get; set; } = null!;
    public NpcRace? NpcRace { get; set; }
    public NpcSex NpcSex { get; set; } = null!;
    public NpcStatus? Status { get; set; }
    public NpcStats? Stats { get; set; }
    public NpcStatsVitals? StatsVitals { get; set; }
    public NpcStatsAttack? StatsAttack { get; set; }
    public NpcStatsDefence? StatsDefence { get; set; }
    public NpcStatsSpeed? StatsSpeed { get; set; }
}
