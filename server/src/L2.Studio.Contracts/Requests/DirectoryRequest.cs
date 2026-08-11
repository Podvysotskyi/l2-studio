namespace L2.Studio.Contracts.Requests;

public sealed record DirectoryRequest(string? Query = null, int Page = 1, int PageSize = 25);
