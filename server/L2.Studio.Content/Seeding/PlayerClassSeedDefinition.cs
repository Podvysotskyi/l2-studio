using L2.Studio.Content.Identifiers;

namespace L2.Studio.Content.Seeding;

public sealed record PlayerClassSeedDefinition(
    PlayerClassId Id,
    string Name,
    PlayerClassId? ParentClassId,
    bool IsMage = false,
    IReadOnlyList<PlayerClassRaceSeedDefinition>? Races = null)
{
    public IReadOnlyList<PlayerClassRaceSeedDefinition> AllowedRaces =>
        Races ?? PlayerClassSeedValues.ForCanonicalRace(Id);
}

public sealed record PlayerClassRaceSeedDefinition(
    PlayerRaceId Id,
    IReadOnlyList<PlayerSexId> AllowedSexIds);
