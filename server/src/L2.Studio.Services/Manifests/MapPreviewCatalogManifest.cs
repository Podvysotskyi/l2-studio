namespace L2.Studio.Services;

internal sealed record MapPreviewCatalogManifest(
    int SchemaVersion,
    string Kind,
    string SourceHash,
    int RendererVersion,
    IReadOnlyList<MapPreviewCatalogEntry> Previews);
