using System.Text.Json;
using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface IAssetCatalogRepository
{
    Task<IReadOnlyList<AssetCatalogSummary>> GetSummariesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<AssetCatalogPage?> SearchAsync(string gameVersion, string kind, string query, string? groupName, string? originalFolder, int page, int pageSize, CancellationToken cancellationToken);
    Task<JsonElement?> GetAsync(string gameVersion, string kind, string name, CancellationToken cancellationToken);
}
