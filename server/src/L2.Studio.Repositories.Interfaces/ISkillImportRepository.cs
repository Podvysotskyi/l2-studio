using L2.Studio.Contracts.Responses;

namespace L2.Studio.Repositories.Interfaces;

public interface ISkillImportRepository
{
    Task<SkillImportRunSummary?> QueueAsync(string gameVersion, string mode, CancellationToken cancellationToken);
    Task<IReadOnlyList<SkillImportRunSummary>> GetRecentAsync(string gameVersion, int limit, CancellationToken cancellationToken);
    Task<SkillImportRunSummary?> GetAsync(string gameVersion, Guid id, CancellationToken cancellationToken);
}
