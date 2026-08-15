namespace L2.Studio.Context.Entities;

public sealed class ItemStats
{
    public required string GameVersion { get; set; }
    public int ItemId { get; set; }
    public decimal? AccuracyCombat { get; set; }
    public decimal? CriticalRate { get; set; }
    public decimal? MagicalAttack { get; set; }
    public decimal? MagicalDefence { get; set; }
    public decimal? MaximumMp { get; set; }
    public decimal? PhysicalAttack { get; set; }
    public decimal? PhysicalAttackRange { get; set; }
    public decimal? PhysicalAttackSpeed { get; set; }
    public decimal? PhysicalDefence { get; set; }
    public decimal? Evasion { get; set; }
    public decimal? ShieldRate { get; set; }
    public decimal? RandomDamage { get; set; }
    public decimal? ShieldDefence { get; set; }
    public Item Item { get; set; } = null!;
}
