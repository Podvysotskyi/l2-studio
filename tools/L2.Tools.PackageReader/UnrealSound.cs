namespace L2.Tools.PackageReader;

public sealed record UnrealSoundExport(
    string Name,
    byte[] WaveData,
    int SampleRate,
    int Channels,
    double DurationSeconds);
