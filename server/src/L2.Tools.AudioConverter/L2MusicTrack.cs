using System.Buffers.Binary;

namespace L2.Tools.AudioConverter;

public sealed record L2MusicTrack(
    byte[] Data,
    int SampleRate,
    int Channels,
    double DurationSeconds);
