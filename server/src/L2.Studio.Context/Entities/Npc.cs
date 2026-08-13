namespace L2.Studio.Context.Entities;

public sealed class Npc
{
    public required string GameVersion { get; set; }
    public int Id { get; set; }
    public short Level { get; set; }
    public string? Name { get; set; }
    public required string NpcTypeName { get; set; }
    public string? NpcRaceName { get; set; }
    public required string NpcSexName { get; set; }
    public NpcType NpcType { get; set; } = null!;
    public NpcRace? NpcRace { get; set; }
    public NpcSex NpcSex { get; set; } = null!;
}
