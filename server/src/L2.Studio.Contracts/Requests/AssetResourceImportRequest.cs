namespace L2.Studio.Contracts.Requests;

public sealed record AssetResourceImportRequest(
    string? ResourceName,
    string? PackageName = null,
    string? SourceKey = null,
    bool Force = false);
