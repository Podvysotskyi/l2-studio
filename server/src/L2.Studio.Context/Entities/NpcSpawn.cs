using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_spawns")]
[PrimaryKey(nameof(GameVersion), nameof(Name))]
public sealed class NpcSpawn
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("name"), MaxLength(128)]
    public required string Name { get; set; }
    public ICollection<NpcSpawnEntity> Entities { get; } = [];
}
