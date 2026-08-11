namespace L2.Studio.Contracts;

public sealed record NpcSummary(
    int Id,
    short Level,
    string? Name,
    int NpcTypeId,
    string NpcType,
    int? NpcRaceId,
    string? NpcRace,
    int NpcSexId,
    string NpcSex);

public sealed record NpcDirectoryPage(
    IReadOnlyList<NpcSummary> Items,
    long Total,
    int Page,
    int PageSize);

public sealed record NpcLookupSummary(int Id, string Name);

public sealed record PlayerClassSummary(
    int Id,
    string Name,
    int? ParentClassId,
    bool IsMage,
    IReadOnlyList<PlayerClassRaceSummary> AllowedRaces);

public sealed record PlayerClassRaceSummary(
    int Id,
    string Name,
    IReadOnlyList<PlayerSexSummary> AllowedSexes);

public sealed record PlayerSexSummary(int Id, string Name);

public sealed record PlayerLookupSummary(int Id, string Name);

public sealed record SkillSummary(
    int Id,
    short Levels,
    string Name,
    int? SkillOperateTypeId,
    string? SkillOperateType,
    int? SkillTargetTypeId,
    string? SkillTargetType,
    int IconCount);

public sealed record SkillDirectoryPage(
    IReadOnlyList<SkillSummary> Items,
    long Total,
    int Page,
    int PageSize);

public sealed record SkillLookupSummary(int Id, string Name);
