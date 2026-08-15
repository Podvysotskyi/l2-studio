using L2.Studio.Contracts.Responses;

namespace L2.Studio.Repositories.Interfaces;

public interface IImportJobRepository
{
    Task<ImportJobSummary?> QueueContentAsync(
        string gameVersion,
        string target,
        string mode,
        CancellationToken cancellationToken);

    Task<ImportJobPage> GetPageAsync(
        string gameVersion,
        string? category,
        string? target,
        string? status,
        string? query,
        int page,
        int pageSize,
        CancellationToken cancellationToken);

    Task<ImportJobSummary?> GetAsync(string gameVersion, Guid id, CancellationToken cancellationToken);
}
