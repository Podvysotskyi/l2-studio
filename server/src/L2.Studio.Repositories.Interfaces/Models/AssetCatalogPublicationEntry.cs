namespace L2.Studio.Repositories.Interfaces.Models;

public sealed record AssetCatalogPublicationEntry(
    string Name,
    string? GroupName,
    string? Status,
    string MetadataJson);
