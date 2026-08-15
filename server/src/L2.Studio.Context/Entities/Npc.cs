namespace L2.Studio.Context.Entities;

public sealed class Npc
{
    public required string GameVersion { get; set; }
    public int Id { get; set; }
    public int? AppearanceId { get; set; }
    public short Level { get; set; }
    public string? Name { get; set; }
    public required string NpcTypeName { get; set; }
    public string? NpcRaceName { get; set; }
    public required string NpcSexName { get; set; }
    public NpcType NpcType { get; set; } = null!;
    public NpcRace? NpcRace { get; set; }
    public NpcSex NpcSex { get; set; } = null!;
    public NpcStatus? Status { get; set; }
    public NpcStats? Stats { get; set; }
    public NpcStatsVitals? StatsVitals { get; set; }
    public NpcStatsAttack? StatsAttack { get; set; }
    public NpcStatsDefence? StatsDefence { get; set; }
    public NpcStatsSpeed? StatsSpeed { get; set; }
}
