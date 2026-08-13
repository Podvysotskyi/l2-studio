namespace L2.Studio.Context.Entities;

public sealed class NpcSex
{
    public required string GameVersion { get; set; }
    public required string Name { get; set; }
    public required string DisplayName { get; set; }
    public ICollection<Npc> Npcs { get; set; } = [];
}
