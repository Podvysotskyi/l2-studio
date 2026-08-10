using L2.Studio.Content.Identifiers;

namespace L2.Studio.Content.Entities;

public sealed class Npc
{
    public int Id { get; set; }
    public short Level { get; set; }
    public string? Name { get; set; }
    public NpcTypeId NpcTypeId { get; set; }
    public NpcRaceId? NpcRaceId { get; set; }
    public NpcSexId NpcSexId { get; set; }
    public NpcType NpcType { get; set; } = null!;
    public NpcRace? NpcRace { get; set; }
    public NpcSex NpcSex { get; set; } = null!;
}
