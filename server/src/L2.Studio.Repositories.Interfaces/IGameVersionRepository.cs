using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface IGameVersionRepository
{
    Task<IReadOnlyList<GameVersionSummary>> ListAsync(CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken);
}
