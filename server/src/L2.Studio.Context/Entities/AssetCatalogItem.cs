using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_catalog_items")]
public sealed class AssetCatalogItem
{
    [Key, Column("id")]
    public long Id { get; set; }
    [Column("catalog_id")]
    public Guid CatalogId { get; set; }
    [Column("source_id")]
    public Guid SourceId { get; set; }
    [Column("name"), MaxLength(256)]
    public required string Name { get; set; }
    [Column("group_name"), MaxLength(256)]
    public string? GroupName { get; set; }
    [Column("status"), MaxLength(32)]
    public required string Status { get; set; }
    [Column("metadata_json")]
    public required string MetadataJson { get; set; }
    public AssetCatalog Catalog { get; set; } = null!;
    public AssetCatalogSource Source { get; set; } = null!;
}
