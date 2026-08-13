namespace L2.Studio.Services;

internal sealed record MapPreviewCatalogEntry(
    string Name,
    string MapSourceHash,
    string? ImageUrl,
    int Width,
    int Height,
    string Status,
    string? Error,
    string SourceKey = "");
