namespace L2.Studio.Context.Entities;

public sealed class AssetCatalogItem
{
    public long Id { get; set; }
    public Guid CatalogId { get; set; }
    public Guid SourceId { get; set; }
    public required string Name { get; set; }
    public string? GroupName { get; set; }
    public required string Status { get; set; }
    public required string MetadataJson { get; set; }
    public AssetCatalog Catalog { get; set; } = null!;
    public AssetCatalogSource Source { get; set; } = null!;
}
