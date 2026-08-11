using System.Text.Json;
using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface IAssetCatalogRepository
{
    Task<IReadOnlyList<AssetCatalogSummary>> GetSummariesAsync(CancellationToken cancellationToken);
    Task<AssetCatalogPage?> SearchAsync(string kind, string query, string? groupName, int page, int pageSize, CancellationToken cancellationToken);
    Task<JsonElement?> GetAsync(string kind, string name, CancellationToken cancellationToken);
}
