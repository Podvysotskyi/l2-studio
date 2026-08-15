using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("asset_catalogs")]
public sealed class AssetCatalog
{
    [Column("game_version"), MaxLength(32)]
    public string GameVersion { get; set; } = "interlude";
    [Key, Column("id"), DatabaseGenerated(DatabaseGeneratedOption.None)]
    public Guid Id { get; set; }
    [Column("kind"), MaxLength(64)]
    public required string Kind { get; set; }
    [Column("source_folder"), MaxLength(256)]
    public required string SourceFolder { get; set; }
    [Column("source_hash"), MaxLength(64)]
    public required string SourceHash { get; set; }
    [Column("schema_version")]
    public int SchemaVersion { get; set; }
    [Column("protocol")]
    public int? Protocol { get; set; }
    [Column("metadata_json")]
    public required string MetadataJson { get; set; }
    [Column("is_active")]
    public bool IsActive { get; set; }
    [Column("published_at")]
    public DateTimeOffset PublishedAt { get; set; }
    public ICollection<AssetCatalogSource> Sources { get; set; } = [];
    public ICollection<AssetCatalogGroup> Groups { get; set; } = [];
    public ICollection<AssetCatalogItem> Items { get; set; } = [];
}
