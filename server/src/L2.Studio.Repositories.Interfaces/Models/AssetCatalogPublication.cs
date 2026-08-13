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
