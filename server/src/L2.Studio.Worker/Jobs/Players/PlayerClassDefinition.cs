using L2.Studio.Context.Identifiers;

namespace L2.Studio.Worker;

public sealed record PlayerClassDefinition(
    PlayerClassId Id,
    PlayerRaceId RaceId,
    bool IsMage,
    PlayerClassId? ParentClassId,
    string Name);
