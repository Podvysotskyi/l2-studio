using System.Text.Json;

namespace L2.Studio.Contracts;

public sealed record AssetCatalogSummary(
    string Kind,
    string SourceFolder,
    string SourceHash,
    int SchemaVersion,
    int? Protocol,
    long Total,
    long Resolved,
    long Skipped,
    long GroupCount,
    DateTimeOffset PublishedAt);
