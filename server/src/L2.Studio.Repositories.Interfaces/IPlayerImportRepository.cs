using L2.Studio.Contracts.Responses;

namespace L2.Studio.Repositories.Interfaces;

public interface IPlayerImportRepository
{
    Task<PlayerImportRunSummary?> QueueAsync(string gameVersion, string mode, CancellationToken cancellationToken);
    Task<IReadOnlyList<PlayerImportRunSummary>> GetRecentAsync(string gameVersion, int limit, CancellationToken cancellationToken);
    Task<PlayerImportRunSummary?> GetAsync(string gameVersion, Guid id, CancellationToken cancellationToken);
}
