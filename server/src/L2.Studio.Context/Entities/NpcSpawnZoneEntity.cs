using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_spawn_zone_entities")]
[PrimaryKey(nameof(GameVersion), nameof(NpcSpawnZoneName), nameof(Sequence))]
public sealed class NpcSpawnZoneEntity
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_spawn_zone_name"), MaxLength(128)]
    public required string NpcSpawnZoneName { get; set; }
    [Column("sequence")]
    public int Sequence { get; set; }
    [Column("npc_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NpcId { get; set; }
    [Column("count")]
    public int Count { get; set; }
    [Column("respawn_delay_seconds")]
    public int RespawnDelaySeconds { get; set; }
    [Column("respawn_random_seconds")]
    public int? RespawnRandomSeconds { get; set; }
    public NpcSpawnZone NpcSpawnZone { get; set; } = null!;
}
