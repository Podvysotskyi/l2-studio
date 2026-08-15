namespace L2.Studio.Context.Entities;

public sealed class NpcStatsVitals : INpcStatsRecord
{
    public required string GameVersion { get; set; }
    public int NpcId { get; set; }
    public decimal? Hp { get; set; }
    public decimal? HpRegen { get; set; }
    public decimal? Mp { get; set; }
    public decimal? MpRegen { get; set; }
    public Npc Npc { get; set; } = null!;
}
