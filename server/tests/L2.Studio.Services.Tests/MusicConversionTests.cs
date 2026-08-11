using L2.Tools.AudioConverter;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class MusicConversionTests
{
    [Fact]
    public void L2SD_music_decodes_to_a_valid_ogg_vorbis_stream()
    {
        var root = FindRepositoryRoot();
        var source = Path.Combine(root, "sources", "Interlude", "music", "b01_f.ogg");
        if (!File.Exists(source))
        {
            return;
        }

        var track = L2MusicDecoder.Decode(File.ReadAllBytes(source));

        Assert.Equal("OggS", System.Text.Encoding.ASCII.GetString(track.Data, 0, 4));
        Assert.Equal(44_100, track.SampleRate);
        Assert.Equal(2, track.Channels);
        Assert.InRange(track.DurationSeconds, 60, 600);
    }

    [Fact]
    public void Decoder_rejects_non_L2SD_input()
    {
        var exception = Assert.Throws<InvalidDataException>(() =>
            L2MusicDecoder.Decode("OggS-invalid"u8));

        Assert.Contains("L2SD", exception.Message);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "compose.yaml")) && Directory.Exists(Path.Combine(directory.FullName, "l2-studio")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
