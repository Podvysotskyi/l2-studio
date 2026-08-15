namespace L2.Studio.Contracts.Requests;

public record DirectoryRequest(string? Query = null, int Page = 1, int PageSize = 25);
