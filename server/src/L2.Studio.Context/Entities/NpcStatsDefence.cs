namespace L2.Studio.Context.Entities;

public sealed class NpcStatsDefence : INpcStatsRecord
{
    public required string GameVersion { get; set; }
    public int NpcId { get; set; }
    public decimal? Physical { get; set; }
    public decimal? Magical { get; set; }
    public int? Evasion { get; set; }
    public int? Shield { get; set; }
    public int? ShieldRate { get; set; }
    public Npc Npc { get; set; } = null!;
}
