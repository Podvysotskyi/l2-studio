namespace L2.Studio.Contracts;

public sealed record AssetImportDiagnosticSummary(
    long Id,
    Guid RunId,
    Guid? WorkItemId,
    string Severity,
    string Code,
    string Stage,
    string? SourceKey,
    string? ObjectName,
    string Message,
    DateTimeOffset CreatedAt);
