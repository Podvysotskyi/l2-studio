using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface IAssetImportRepository
{
    Task<AssetImportRunSummary?> QueueFullScanAsync(string kind, CancellationToken cancellationToken);
    Task<AssetImportRunSummary?> QueueSingleFileAsync(string kind, string fileName, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetImportRunSummary>> GetRecentAsync(string kind, int limit, CancellationToken cancellationToken);
    Task<AssetImportRunSummary?> GetAsync(Guid id, string kind, CancellationToken cancellationToken);
    Task<AssetImportWorkItemPage?> GetWorkItemsAsync(
        Guid runId, string kind, string? sourceKey, string? status, int page, int pageSize,
        CancellationToken cancellationToken);
    Task<AssetImportDiagnosticPage?> GetDiagnosticsAsync(
        Guid runId, string kind, string? sourceKey, string? severity, string? code, string? stage,
        string? workItemStatus, string? query, int page, int pageSize, CancellationToken cancellationToken);
}
