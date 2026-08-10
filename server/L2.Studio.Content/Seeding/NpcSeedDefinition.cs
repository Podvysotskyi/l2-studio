using L2.Studio.Content.Identifiers;

namespace L2.Studio.Content.Seeding;

public sealed record NpcSeedDefinition(
    int Id,
    short Level,
    string? Name,
    NpcTypeId NpcTypeId,
    NpcRaceId? NpcRaceId,
    NpcSexId NpcSexId);
