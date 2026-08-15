using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace L2.Studio.Context.Entities;

[Table("item_stats")]
[PrimaryKey(nameof(GameVersion), nameof(ItemId))]
public sealed class ItemStats
{
    [Column("game_version"), MaxLength(32)]
    public required string GameVersion { get; set; }
    [Column("item_id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ItemId { get; set; }
    [Column("accuracy_combat")]
    public decimal? AccuracyCombat { get; set; }
    [Column("critical_rate")]
    public decimal? CriticalRate { get; set; }
    [Column("magical_attack")]
    public decimal? MagicalAttack { get; set; }
    [Column("magical_defence")]
    public decimal? MagicalDefence { get; set; }
    [Column("maximum_mp")]
    public decimal? MaximumMp { get; set; }
    [Column("physical_attack")]
    public decimal? PhysicalAttack { get; set; }
    [Column("physical_attack_range")]
    public decimal? PhysicalAttackRange { get; set; }
    [Column("physical_attack_speed")]
    public decimal? PhysicalAttackSpeed { get; set; }
    [Column("physical_defence")]
    public decimal? PhysicalDefence { get; set; }
    [Column("evasion")]
    public decimal? Evasion { get; set; }
    [Column("shield_rate")]
    public decimal? ShieldRate { get; set; }
    [Column("random_damage")]
    public decimal? RandomDamage { get; set; }
    [Column("shield_defence")]
    public decimal? ShieldDefence { get; set; }
    public Item Item { get; set; } = null!;
}
