namespace L2.Studio.Contracts;

public sealed record AssetCatalogDiagnosticPage(
    Guid RunId,
    Guid WorkItemId,
    string SourceKey,
    string WorkItemStatus,
    DateTimeOffset PublishedAt,
    IReadOnlyList<AssetImportDiagnosticSummary> Items,
    long Total,
    int Page,
    int PageSize);
