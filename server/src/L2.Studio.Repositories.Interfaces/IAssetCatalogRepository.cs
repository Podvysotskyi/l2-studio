using System.Text.Json;
using L2.Studio.Contracts;

namespace L2.Studio.Repositories.Interfaces;

public interface IAssetCatalogRepository
{
    Task<IReadOnlyList<AssetCatalogSummary>> GetSummariesAsync(string gameVersion, CancellationToken cancellationToken);
    Task<NpcAppearanceManifestReference?> GetNpcAppearanceManifestAsync(
        string gameVersion,
        int npcId,
        CancellationToken cancellationToken);
    Task<AssetCatalogPage?> SearchAsync(string gameVersion, string kind, string query, string? groupName, string? originalFolder, int page, int pageSize, CancellationToken cancellationToken);
    Task<JsonElement?> GetAsync(string gameVersion, string kind, string name, string? sourceKey, CancellationToken cancellationToken);
    Task<AssetCatalogDiagnosticPage?> GetDiagnosticsAsync(string gameVersion, string kind, string name,
        string? sourceKey, string? severity, string? query, int page, int pageSize, CancellationToken cancellationToken);
    Task<AssetArtifactPage> GetArtifactsAsync(string gameVersion, string? kind, string? sourceKey,
        bool? current, string? integrityStatus, int page, int pageSize, CancellationToken cancellationToken);
    Task<AssetArtifactDetail?> GetArtifactAsync(string gameVersion, Guid id, CancellationToken cancellationToken);
    Task<AssetArtifactDetail?> VerifyArtifactAsync(string gameVersion, Guid id, CancellationToken cancellationToken);
}
