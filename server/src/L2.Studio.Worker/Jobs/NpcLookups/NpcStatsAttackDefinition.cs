namespace L2.Studio.Worker;

public sealed record NpcStatsAttackDefinition(decimal? Physical, decimal? Magical, int? Random, int? Critical, decimal? Accuracy, int? AttackSpeed, int? ReuseDelay, string? Type, int? Range, int? Distance, int? Width);
