namespace L2.Studio.Context.Entities;

public sealed class NpcStatus
{
    public required string GameVersion { get; set; }
    public int NpcId { get; set; }
    public bool Attackable { get; set; }
    public bool Targetable { get; set; }
    public bool Talkable { get; set; }
    public bool Undying { get; set; }
    public bool ShowName { get; set; }
    public bool RandomWalk { get; set; }
    public bool CanMove { get; set; }
    public bool NoSleepMode { get; set; }
    public bool CanBeSown { get; set; }
    public Npc Npc { get; set; } = null!;
}
