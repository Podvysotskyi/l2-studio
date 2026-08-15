namespace L2.Studio.Context.Entities;

public sealed class NpcStatsAttack : INpcStatsRecord
{
    public required string GameVersion { get; set; }
    public int NpcId { get; set; }
    public decimal? Physical { get; set; }
    public decimal? Magical { get; set; }
    public int? Random { get; set; }
    public int? Critical { get; set; }
    public decimal? Accuracy { get; set; }
    public int? AttackSpeed { get; set; }
    public int? ReuseDelay { get; set; }
    public string? Type { get; set; }
    public int? Range { get; set; }
    public int? Distance { get; set; }
    public int? Width { get; set; }
    public Npc Npc { get; set; } = null!;
}
