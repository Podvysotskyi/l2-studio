namespace L2.Studio.Contracts;

public sealed record AssetImportDiagnosticPage(
    IReadOnlyList<AssetImportDiagnosticSummary> Items,
    long Total,
    int Page,
    int PageSize);
