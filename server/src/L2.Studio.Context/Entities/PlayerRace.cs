using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using L2.Studio.Context.Identifiers;

namespace L2.Studio.Context.Entities;

[Table("player_races")]
[PrimaryKey(nameof(GameVersion), nameof(Id))]
public sealed class PlayerRace
{
    [Column("game_version"), MaxLength(32)]
    public string GameVersion { get; set; } = "interlude";
    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public PlayerRaceId Id { get; set; }
    [Column("name"), MaxLength(64)]
    public required string Name { get; set; }
    public ICollection<PlayerClass> PlayerClasses { get; set; } = [];
    public ICollection<PlayerFace> PlayerFaces { get; set; } = [];
    public ICollection<PlayerHairStyle> PlayerHairStyles { get; set; } = [];
    public ICollection<PlayerHairColor> PlayerHairColors { get; set; } = [];
}
