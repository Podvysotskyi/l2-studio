namespace L2.Studio.Services;

internal sealed record MusicManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    IReadOnlyList<MusicManifestEntry> Tracks);
