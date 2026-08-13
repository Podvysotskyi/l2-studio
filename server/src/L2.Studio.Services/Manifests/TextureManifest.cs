namespace L2.Studio.Services;

internal sealed record TextureManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<TextureManifestPackage> Packages,
    IReadOnlyList<TextureManifestEntry> Textures,
    IReadOnlyList<TextureMaterialManifestEntry>? Materials);
