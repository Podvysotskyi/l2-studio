namespace L2.Studio.Services;

internal sealed record SoundManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<SoundManifestEntry> Sounds);
