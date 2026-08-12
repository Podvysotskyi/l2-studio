using L2.Studio.Context.Identifiers;

namespace L2.Studio.Context.Entities;

public sealed class NpcSex
{
    public string GameVersion { get; set; } = "interlude";
    public NpcSexId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Npc> Npcs { get; set; } = [];
}
