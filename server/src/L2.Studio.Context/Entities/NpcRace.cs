using L2.Studio.Context.Identifiers;

namespace L2.Studio.Context.Entities;

public sealed class NpcRace
{
    public string GameVersion { get; set; } = "interlude";
    public NpcRaceId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<Npc> Npcs { get; set; } = [];
}
