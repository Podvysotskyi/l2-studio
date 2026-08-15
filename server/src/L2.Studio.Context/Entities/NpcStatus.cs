using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("npc_statuses")]
[PrimaryKey(nameof(GameVersion), nameof(NpcId))]
public sealed class NpcStatus
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("npc_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int NpcId { get; set; }
    [Column("attackable")]
    public bool Attackable { get; set; }
    [Column("targetable")]
    public bool Targetable { get; set; }
    [Column("talkable")]
    public bool Talkable { get; set; }
    [Column("undying")]
    public bool Undying { get; set; }
    [Column("show_name")]
    public bool ShowName { get; set; }
    [Column("random_walk")]
    public bool RandomWalk { get; set; }
    [Column("can_move")]
    public bool CanMove { get; set; }
    [Column("no_sleep_mode")]
    public bool NoSleepMode { get; set; }
    [Column("can_be_sown")]
    public bool CanBeSown { get; set; }
    public Npc Npc { get; set; } = null!;
}
