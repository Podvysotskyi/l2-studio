using L2.Studio.Context.Identifiers;

namespace L2.Studio.Context.Entities;

public sealed class PlayerRace
{
    public PlayerRaceId Id { get; set; }
    public required string Name { get; set; }
    public ICollection<PlayerClass> PlayerClasses { get; set; } = [];
    public ICollection<PlayerFace> PlayerFaces { get; set; } = [];
    public ICollection<PlayerHairStyle> PlayerHairStyles { get; set; } = [];
    public ICollection<PlayerHairColor> PlayerHairColors { get; set; } = [];
}
