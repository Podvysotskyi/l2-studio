namespace L2.Studio.Contracts.Requests;

public sealed record NpcDirectoryRequest(
    string? Query = null,
    int Page = 1,
    int PageSize = 25,
    string? NpcTypeName = null,
    string? NpcRaceName = null,
    bool? WithoutRace = null,
    string? NpcSexName = null,
    bool? HasVisuals = null) : DirectoryRequest(Query, Page, PageSize);
