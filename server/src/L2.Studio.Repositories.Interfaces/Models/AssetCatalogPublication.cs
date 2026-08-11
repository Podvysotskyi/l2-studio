namespace L2.Studio.Repositories.Interfaces.Models;

public sealed record AssetCatalogPublication(
    Guid WorkItemId,
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
    string MetadataJson,
    IReadOnlyList<string> Warnings,
    DateTimeOffset PublishedAt);

public sealed record AssetCatalogPublicationEntry(
    string Name,
    string? GroupName,
    string? Status,
    string MetadataJson);
