namespace L2.Studio.Context.Entities;

public interface INpcStatsRecord
{
    string GameVersion { get; set; }
    int NpcId { get; set; }
    Npc Npc { get; set; }
}
