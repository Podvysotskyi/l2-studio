namespace L2.Studio.Contracts.Requests;

public sealed record AssetImportRequest(bool Force = false, string? MapName = null);
