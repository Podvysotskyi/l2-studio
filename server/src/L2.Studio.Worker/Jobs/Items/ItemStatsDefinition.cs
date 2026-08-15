namespace L2.Studio.Worker;

public sealed record ItemStatsDefinition(
    decimal? AccuracyCombat, decimal? CriticalRate, decimal? MagicalAttack, decimal? MagicalDefence,
    decimal? MaximumMp, decimal? PhysicalAttack, decimal? PhysicalAttackRange, decimal? PhysicalAttackSpeed,
    decimal? PhysicalDefence, decimal? Evasion, decimal? ShieldRate, decimal? RandomDamage, decimal? ShieldDefence);
