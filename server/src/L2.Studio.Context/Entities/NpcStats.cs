namespace L2.Studio.Context.Entities;

public sealed class NpcStats : INpcStatsRecord
{
    public required string GameVersion { get; set; }
    public int NpcId { get; set; }
    public int? Str { get; set; }
    public int? Int { get; set; }
    public int? Dex { get; set; }
    public int? Wit { get; set; }
    public int? Con { get; set; }
    public int? Men { get; set; }
    public int? HitTime { get; set; }
    public Npc Npc { get; set; } = null!;
}
