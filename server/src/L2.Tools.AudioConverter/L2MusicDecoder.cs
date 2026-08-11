using System.Buffers.Binary;

namespace L2.Tools.AudioConverter;

public static class L2MusicDecoder
{
    private const int LineageTrailerSize = 20;
    private static readonly byte[] LineageSignature = "L2SD"u8.ToArray();
    private static readonly byte[] OggSignature = "OggS"u8.ToArray();
    private static readonly byte[] VorbisSignature = "vorbis"u8.ToArray();

    public static L2MusicTrack Decode(ReadOnlySpan<byte> input)
    {
        if (input.Length < 28 || !input[..4].SequenceEqual(LineageSignature))
        {
            throw new InvalidDataException("The music file does not have an L2SD Ogg signature.");
        }

        var decoded = input.ToArray();
        OggSignature.CopyTo(decoded, 0);
        var position = 0;
        var sampleRate = 0;
        var channels = 0;
        ulong finalGranule = 0;

        while (position < decoded.Length)
        {
            if (decoded.Length - position == LineageTrailerSize)
            {
                break;
            }

            if (decoded.Length - position < 27 ||
                !decoded.AsSpan(position, 4).SequenceEqual(OggSignature))
            {
                throw new InvalidDataException($"The music file has an invalid Ogg page at byte {position}.");
            }

            var pageSegmentCount = decoded[position + 26];
            var segmentTableOffset = position + 27;
            var pageBodyOffset = segmentTableOffset + pageSegmentCount;
            if (pageBodyOffset > decoded.Length)
            {
                throw new InvalidDataException("The music file has a truncated Ogg segment table.");
            }

            var pageBodyLength = 0;
            for (var index = 0; index < pageSegmentCount; index++)
            {
                pageBodyLength += decoded[segmentTableOffset + index];
            }

            var nextPage = pageBodyOffset + pageBodyLength;
            if (nextPage > decoded.Length)
            {
                throw new InvalidDataException("The music file has a truncated Ogg page body.");
            }

            var granule = BinaryPrimitives.ReadUInt64LittleEndian(decoded.AsSpan(position + 6, 8));
            if (granule != ulong.MaxValue)
            {
                finalGranule = Math.Max(finalGranule, granule);
            }

            if (sampleRate == 0 && pageBodyLength >= 16 &&
                decoded[pageBodyOffset] == 1 &&
                decoded.AsSpan(pageBodyOffset + 1, 6).SequenceEqual(VorbisSignature))
            {
                channels = decoded[pageBodyOffset + 11];
                sampleRate = BinaryPrimitives.ReadInt32LittleEndian(decoded.AsSpan(pageBodyOffset + 12, 4));
            }

            position = nextPage;
        }

        if (sampleRate <= 0 || channels <= 0)
        {
            throw new InvalidDataException("The music file has no valid Vorbis identification packet.");
        }

        if (decoded.Length - position != LineageTrailerSize)
        {
            throw new InvalidDataException("The music file does not have the expected Lineage trailer.");
        }

        return new L2MusicTrack(
            decoded[..position],
            sampleRate,
            channels,
            finalGranule / (double)sampleRate);
    }
}

public sealed record L2MusicTrack(
    byte[] Data,
    int SampleRate,
    int Channels,
    double DurationSeconds);
