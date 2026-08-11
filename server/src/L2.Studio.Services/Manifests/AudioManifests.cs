namespace L2.Studio.Services;

internal sealed record MusicManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    IReadOnlyList<MusicManifestEntry> Tracks);

internal sealed record MusicManifestEntry(
    string Name,
    string FileName,
    string? Url,
    double? DurationSeconds,
    int? SampleRate,
    int? Channels,
    long SizeBytes,
    string? Sha256,
    string Status,
    string? Error);

internal sealed record SoundManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<SoundManifestEntry> Sounds);

internal sealed record SoundManifestEntry(
    string PackageName,
    string ObjectName,
    string Url,
    double DurationSeconds,
    int SampleRate,
    int Channels,
    long SizeBytes,
    string Sha256);

