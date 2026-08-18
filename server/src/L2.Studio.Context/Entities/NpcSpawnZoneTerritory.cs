using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_spawn_zone_territories")]
[PrimaryKey(nameof(GameVersion), nameof(NpcSpawnZoneName))]
public sealed class NpcSpawnZoneTerritory
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_spawn_zone_name"), MaxLength(128)]
    public required string NpcSpawnZoneName { get; set; }
    [Column("min_z")]
    public short MinZ { get; set; }
    [Column("max_z")]
    public short MaxZ { get; set; }
    public NpcSpawnZone NpcSpawnZone { get; set; } = null!;
    public ICollection<NpcSpawnZoneTerritoryNode> Nodes { get; } = [];
}
