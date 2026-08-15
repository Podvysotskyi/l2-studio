namespace L2.Studio.Context.Entities;

public sealed class NpcStatsSpeed : INpcStatsRecord
{
    public required string GameVersion { get; set; }
    public int NpcId { get; set; }
    public decimal? WalkGround { get; set; }
    public decimal? RunGround { get; set; }
    public Npc Npc { get; set; } = null!;
}
