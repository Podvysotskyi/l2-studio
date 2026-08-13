namespace L2.Studio.Services;

internal sealed record SoundManifestEntry(
    string PackageName,
    string ObjectName,
    string Url,
    double DurationSeconds,
    int SampleRate,
    int Channels,
    long SizeBytes,
    string Sha256);
