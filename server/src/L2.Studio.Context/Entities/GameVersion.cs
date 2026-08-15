using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

[Table("game_versions")]
public sealed class GameVersion
{
    [Key, Column("key"), MaxLength(32)]
    public string Key { get; set; } = string.Empty;
    [Column("display_name"), MaxLength(64)]
    public required string DisplayName { get; set; }
    [Column("source_folder"), MaxLength(64)]
    public required string SourceFolder { get; set; }
    [Column("sort_order")]
    public int SortOrder { get; set; }
}
