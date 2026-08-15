namespace L2.Studio.Contracts.Requests;

public sealed record PlayerAppearanceDirectoryRequest(
    string? Query = null,
    int Page = 1,
    int PageSize = 25,
    int? PlayerRaceId = null,
    int? PlayerSexId = null) : DirectoryRequest(Query, Page, PageSize);
