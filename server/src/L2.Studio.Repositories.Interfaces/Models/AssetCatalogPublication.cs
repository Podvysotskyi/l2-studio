namespace L2.Studio.Repositories.Interfaces.Models;

public sealed record AssetCatalogPublication(
    Guid WorkItemId,
    string GameVersion,
    string Kind,
    string SourceKey,
    string NormalizedSourceKey,
    string SourceFolder,
    string SourceHash,
    string OutputRoot,
    int SchemaVersion,
    int? Protocol,
    IReadOnlyList<AssetCatalogPublicationEntry> Groups,
    IReadOnlyList<AssetCatalogPublicationEntry> Items,
    IReadOnlyList<AssetCatalogDependencyPublication> Dependencies,
    IReadOnlyList<AssetArtifactFilePublication> Files,
    string RecipeVersion,
    string ContentHash,
    string MetadataJson,
    IReadOnlyList<string> Warnings,
    DateTimeOffset PublishedAt);

public sealed record AssetCatalogPublicationEntry(
    string Name,
    string? GroupName,
    string? Status,
    string MetadataJson);

public sealed record AssetCatalogDependencyPublication(
    string Kind,
    string DependencyKey,
    string? ResolvedSourceKey,
    string? ArtifactFingerprint,
    bool IsResolved,
    string? OutputRoot);

public sealed record AssetArtifactFilePublication(
    string RelativePath,
    string PublicPath,
    string Role,
    string MediaType,
    long SizeBytes,
    string Sha256);
