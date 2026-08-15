namespace L2.Studio.Contracts.Requests;

public sealed record ItemDirectoryRequest(
    string? Query = null,
    int Page = 1,
    int PageSize = 25,
    string? ItemTypeName = null,
    string? ItemActionName = null,
    string? ItemBodyPartName = null,
    string? ItemMaterialName = null,
    string? ItemCrystalTypeName = null) : DirectoryRequest(Query, Page, PageSize);
