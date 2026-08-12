using L2.Studio.Contracts;
using L2.Studio.Contracts.Requests;

namespace L2.Studio.Repositories.Interfaces;

public interface IAssetReleaseRepository
{
    Task<AssetReleasePage> ListAsync(string gameVersion, string? status, int page, int pageSize, CancellationToken token);
    Task<AssetReleaseDetail?> GetAsync(string gameVersion, Guid id, CancellationToken token);
    Task<AssetReleaseDetail> CreateAsync(string gameVersion, CreateAssetReleaseRequest request, CancellationToken token);
    Task<AssetReleaseDetail?> CloneAsync(string gameVersion, Guid id, CreateAssetReleaseRequest request, CancellationToken token);
    Task<AssetReleaseDetail?> UpdateAsync(string gameVersion, Guid id, UpdateAssetReleaseRequest request, CancellationToken token);
    Task<AssetReleaseDetail?> RefreshAsync(string gameVersion, Guid id, CancellationToken token);
    Task<bool> DeleteDraftAsync(string gameVersion, Guid id, CancellationToken token);
    Task<AssetReleaseDetail?> QueueValidationAsync(string gameVersion, Guid id, CancellationToken token);
    Task ValidateAsync(Guid id, CancellationToken token);
    Task<AssetReleaseDetail?> PublishAsync(string gameVersion, Guid id, CancellationToken token);
    Task<AssetReleaseDetail?> QueueActivationAsync(string gameVersion, Guid id, CancellationToken token);
    Task ActivateAsync(string gameVersion, Guid id, CancellationToken token);
    Task<AssetReleaseDetail?> RetireAsync(string gameVersion, Guid id, CancellationToken token);
    Task<AssetReleaseResourcePage?> SearchResourcesAsync(string gameVersion, Guid id, string type, string query,
        int page, int pageSize, CancellationToken token);
}
