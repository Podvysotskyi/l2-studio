using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface IAssetImportRepository
{
    Task<AssetImportJobSummary?> QueueAsync(string kind, string? levelName, CancellationToken cancellationToken);
    Task<IReadOnlyList<AssetImportJobSummary>> GetRecentAsync(string kind, int limit, CancellationToken cancellationToken);
    Task<AssetImportJobSummary?> GetAsync(Guid id, string kind, CancellationToken cancellationToken);
}
