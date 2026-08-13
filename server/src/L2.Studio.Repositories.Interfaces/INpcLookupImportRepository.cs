using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface INpcLookupImportRepository
{
    Task<NpcLookupImportRunSummary?> QueueAsync(string gameVersion, string kind, CancellationToken cancellationToken);
    Task<IReadOnlyList<NpcLookupImportRunSummary>> GetRecentAsync(string gameVersion, string kind, int limit, CancellationToken cancellationToken);
    Task<NpcLookupImportRunSummary?> GetAsync(string gameVersion, string kind, Guid id, CancellationToken cancellationToken);
}
