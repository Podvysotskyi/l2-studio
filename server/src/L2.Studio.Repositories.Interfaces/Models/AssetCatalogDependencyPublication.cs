namespace L2.Studio.Repositories.Interfaces.Models;

public sealed record AssetCatalogDependencyPublication(
    string Kind,
    string DependencyKey,
    string? ResolvedSourceKey,
    string? ArtifactFingerprint,
    bool IsResolved,
    string? OutputRoot);
