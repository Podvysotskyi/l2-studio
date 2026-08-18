using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_spawn_entities")]
[PrimaryKey(nameof(GameVersion), nameof(NpcSpawnName), nameof(Sequence))]
public sealed class NpcSpawnEntity
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_spawn_name"), MaxLength(128)]
    public required string NpcSpawnName { get; set; }
    [Column("sequence")]
    public int Sequence { get; set; }
    [Column("npc_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NpcId { get; set; }
    [Column("x")]
    public int X { get; set; }
    [Column("y")]
    public int Y { get; set; }
    [Column("z")]
    public int Z { get; set; }
    [Column("heading")]
    public int Heading { get; set; }
    [Column("respawn_delay_seconds")]
    public int RespawnDelaySeconds { get; set; }
    public NpcSpawn NpcSpawn { get; set; } = null!;
}
