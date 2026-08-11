namespace L2.Studio.Repositories.Interfaces.Models;

public sealed record AssetCatalogPublication(
    Guid Id,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int SchemaVersion,
    int? Protocol,
    IReadOnlyList<AssetCatalogPublicationEntry> Groups,
    IReadOnlyList<AssetCatalogPublicationEntry> Items,
    string MetadataJson,
    DateTimeOffset PublishedAt);

public sealed record AssetCatalogPublicationEntry(
    string Name,
    string? GroupName,
    string? Status,
    string MetadataJson);
