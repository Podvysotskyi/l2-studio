using L2.Studio.Contracts.Responses;

namespace L2.Studio.Repositories.Interfaces;

public interface IItemImportRepository
{
    Task<ItemImportRunSummary?> QueueAsync(string gameVersion, string mode, CancellationToken cancellationToken);
    Task<IReadOnlyList<ItemImportRunSummary>> GetRecentAsync(string gameVersion, int limit, CancellationToken cancellationToken);
    Task<ItemImportRunSummary?> GetAsync(string gameVersion, Guid id, CancellationToken cancellationToken);
}
