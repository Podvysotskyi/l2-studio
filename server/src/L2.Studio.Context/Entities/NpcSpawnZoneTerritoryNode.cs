using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_spawn_zone_territory_nodes")]
[PrimaryKey(nameof(GameVersion), nameof(NpcSpawnZoneName), nameof(Sequence))]
public sealed class NpcSpawnZoneTerritoryNode
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_spawn_zone_name"), MaxLength(128)]
    public required string NpcSpawnZoneName { get; set; }
    [Column("sequence")]
    public int Sequence { get; set; }
    [Column("x")]
    public int X { get; set; }
    [Column("y")]
    public int Y { get; set; }
    public NpcSpawnZoneTerritory Territory { get; set; } = null!;
}
