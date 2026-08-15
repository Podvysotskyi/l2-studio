using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace L2.Studio.Context.Entities;

public sealed class ContentImportRun : ImportJob
{
    [Column("concurrency_key"), MaxLength(64)]
    public required string ConcurrencyKey { get; set; }
    [Column("mode"), MaxLength(32)]
    public required string Mode { get; set; }
    [Column("total_count")]
    public int TotalCount { get; set; }
    [Column("inserted_count")]
    public int InsertedCount { get; set; }
    [Column("existing_count")]
    public int ExistingCount { get; set; }
    [Column("restored_count")]
    public int RestoredCount { get; set; }
}
