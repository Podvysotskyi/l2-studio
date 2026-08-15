namespace L2.Studio.Contracts;

public sealed record NpcStatsAttackSummary(decimal? Physical, decimal? Magical, int? Random, int? Critical, decimal? Accuracy, int? AttackSpeed, int? ReuseDelay, string? Type, int? Range, int? Distance, int? Width);
