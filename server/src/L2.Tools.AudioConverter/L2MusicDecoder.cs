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
        if (input.Length < 28)
        {
            throw new InvalidDataException("The music file does not have an L2SD or OggS signature.");
        }

        var decoded = input.ToArray();
        var hasLineageSignature = input[..4].SequenceEqual(LineageSignature);
        if (hasLineageSignature)
        {
            OggSignature.CopyTo(decoded, 0);
        }
        else if (!input[..4].SequenceEqual(OggSignature))
        {
            throw new InvalidDataException("The music file does not have an L2SD or OggS signature.");
        }

        var position = 0;
        var sampleRate = 0;
        var channels = 0;
        var hasLineageTrailer = false;
        ulong finalGranule = 0;

        while (position < decoded.Length)
        {
            if (decoded.Length - position == LineageTrailerSize)
            {
                hasLineageTrailer = true;
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

        if (hasLineageSignature && !hasLineageTrailer)
        {
            throw new InvalidDataException("The music file does not have the expected Lineage trailer.");
        }

        return new L2MusicTrack(
            hasLineageTrailer ? decoded[..position] : decoded,
            sampleRate,
            channels,
            finalGranule / (double)sampleRate);
    }
}
