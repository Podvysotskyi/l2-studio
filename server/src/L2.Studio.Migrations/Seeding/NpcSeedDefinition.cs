using L2.Studio.Context.Identifiers;

namespace L2.Studio.Migrations.Seeding;

public sealed record NpcSeedDefinition(
    int Id,
    short Level,
    string? Name,
    NpcTypeId NpcTypeId,
    NpcRaceId? NpcRaceId,
    NpcSexId NpcSexId);
