using System.Buffers.Binary;
using L2.Tools.AudioConverter;
using Xunit;

namespace L2.Studio.Services.Tests;

public sealed class L2MusicDecoderTests
{
    [Fact]
    public void DecodeRestoresLineageOggAndRemovesTrailer()
    {
        var expected = CreateOgg();
        var input = expected.Concat(new byte[20]).ToArray();
        "L2SD"u8.CopyTo(input);

        var track = L2MusicDecoder.Decode(input);

        Assert.Equal(expected, track.Data);
        Assert.Equal(48000, track.SampleRate);
        Assert.Equal(2, track.Channels);
        Assert.Equal(1, track.DurationSeconds);
    }

    [Fact]
    public void DecodePreservesStandardOgg()
    {
        var input = CreateOgg();

        var track = L2MusicDecoder.Decode(input);

        Assert.Equal(input, track.Data);
        Assert.Equal(48000, track.SampleRate);
        Assert.Equal(2, track.Channels);
        Assert.Equal(1, track.DurationSeconds);
    }

    [Fact]
    public void DecodeRemovesLineageTrailerFromStandardOgg()
    {
        var expected = CreateOgg();
        var input = expected.Concat(new byte[20]).ToArray();

        var track = L2MusicDecoder.Decode(input);

        Assert.Equal(expected, track.Data);
        Assert.Equal(48000, track.SampleRate);
        Assert.Equal(2, track.Channels);
        Assert.Equal(1, track.DurationSeconds);
    }

    [Fact]
    public void DecodeRejectsUnsupportedSignature()
    {
        var exception = Assert.Throws<InvalidDataException>(() => L2MusicDecoder.Decode(new byte[28]));

        Assert.Equal("The music file does not have an L2SD or OggS signature.", exception.Message);
    }

    [Fact]
    public void DecodeRejectsLineageOggWithoutTrailer()
    {
        var input = CreateOgg();
        "L2SD"u8.CopyTo(input);

        var exception = Assert.Throws<InvalidDataException>(() => L2MusicDecoder.Decode(input));

        Assert.Equal("The music file does not have the expected Lineage trailer.", exception.Message);
    }

    [Fact]
    public void DecodeRejectsUnexpectedTrailingData()
    {
        var input = CreateOgg().Concat(new byte[19]).ToArray();

        var exception = Assert.Throws<InvalidDataException>(() => L2MusicDecoder.Decode(input));

        Assert.Equal("The music file has an invalid Ogg page at byte 44.", exception.Message);
    }

    private static byte[] CreateOgg()
    {
        var ogg = new byte[44];
        "OggS"u8.CopyTo(ogg);
        BinaryPrimitives.WriteUInt64LittleEndian(ogg.AsSpan(6, 8), 48000);
        ogg[26] = 1;
        ogg[27] = 16;
        ogg[28] = 1;
        "vorbis"u8.CopyTo(ogg.AsSpan(29));
        ogg[39] = 2;
        BinaryPrimitives.WriteInt32LittleEndian(ogg.AsSpan(40, 4), 48000);
        return ogg;
    }
}
