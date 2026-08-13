namespace L2.Studio.Services;

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
    string? Error,
    string SourceKey = "");
