using L2.Studio.Content.Identifiers;

namespace L2.Studio.Content.Entities;

public sealed class NpcSex
{
    public NpcSexId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Npc> Npcs { get; set; } = [];
}
