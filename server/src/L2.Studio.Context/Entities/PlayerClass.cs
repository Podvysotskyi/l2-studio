using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

using L2.Studio.Context.Identifiers;

namespace L2.Studio.Context.Entities;

[Table("player_classes")]
[PrimaryKey(nameof(GameVersion), nameof(Id), nameof(PlayerSexId), nameof(PlayerRaceId))]
public sealed class PlayerClass
{
    [Column("game_version"), MaxLength(32)]
    public string GameVersion { get; set; } = "interlude";
    [Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public PlayerClassId Id { get; set; }
    [Column("player_sex_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public PlayerSexId PlayerSexId { get; set; }
    [Column("player_race_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public PlayerRaceId PlayerRaceId { get; set; }
    [Column("name"), MaxLength(64)]
    public required string Name { get; set; }
    [Column("is_mage")]
    public bool IsMage { get; set; }
    [Column("parent_class_id")]
    public PlayerClassId? ParentClassId { get; set; }
    public PlayerSex PlayerSex { get; set; } = null!;
    public PlayerRace PlayerRace { get; set; } = null!;
    public PlayerClass? ParentClass { get; set; }
    public ICollection<PlayerClass> ChildClasses { get; set; } = [];
}
