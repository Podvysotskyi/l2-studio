using L2.Studio.Context.Identifiers;

namespace L2.Studio.Context.Entities;

public sealed class NpcType
{
    public NpcTypeId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Npc> Npcs { get; set; } = [];
}
