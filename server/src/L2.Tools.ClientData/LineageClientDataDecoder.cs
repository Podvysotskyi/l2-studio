using System.Text;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;

namespace L2.Tools.ClientData;

public static class LineageClientDataDecoder
{
    private const int HeaderSize = 28;
    private const int FooterSize = 20;
    private static readonly byte[] Protocol211Key = Encoding.ASCII.GetBytes("31==-%&@!^+][;'.]94-\0");

    public static byte[] DecodeProtocol211(ReadOnlySpan<byte> source)
    {
        if (source.Length < HeaderSize + FooterSize)
            throw new InvalidDataException("The client data file is too short to contain a Lineage header.");
        var header = Encoding.Unicode.GetString(source[..HeaderSize]);
        if (!string.Equals(header, "Lineage2Ver211", StringComparison.Ordinal))
            throw new InvalidDataException($"Expected Lineage2Ver211 but found '{header}'.");

        var encrypted = source[HeaderSize..^FooterSize];

        var result = new byte[encrypted.Length];
        var engine = new BlowfishEngine();
        engine.Init(false, new KeyParameter(Protocol211Key));
        var alignedLength = encrypted.Length / 8 * 8;
        for (var offset = 0; offset < alignedLength; offset += 8)
            ProcessLittleEndianBlock(engine, encrypted.Slice(offset, 8), result.AsSpan(offset, 8));
        encrypted[alignedLength..].CopyTo(result.AsSpan(alignedLength));
        return result;
    }

    internal static byte[] EncodeProtocol211ForTests(ReadOnlySpan<byte> payload)
    {
        var result = new byte[HeaderSize + payload.Length + FooterSize];
        Encoding.Unicode.GetBytes("Lineage2Ver211", result);
        var engine = new BlowfishEngine();
        engine.Init(true, new KeyParameter(Protocol211Key));
        var alignedLength = payload.Length / 8 * 8;
        for (var offset = 0; offset < alignedLength; offset += 8)
            ProcessLittleEndianBlock(engine, payload.Slice(offset, 8), result.AsSpan(HeaderSize + offset, 8));
        payload[alignedLength..].CopyTo(result.AsSpan(HeaderSize + alignedLength));
        return result;
    }

    private static void ProcessLittleEndianBlock(
        BlowfishEngine engine,
        ReadOnlySpan<byte> input,
        Span<byte> output)
    {
        Span<byte> reorderedInput = stackalloc byte[8];
        Span<byte> reorderedOutput = stackalloc byte[8];
        input[..4].CopyTo(reorderedInput[..4]);
        input[4..].CopyTo(reorderedInput[4..]);
        reorderedInput[..4].Reverse();
        reorderedInput[4..].Reverse();
        engine.ProcessBlock(reorderedInput, reorderedOutput);
        reorderedOutput[..4].Reverse();
        reorderedOutput[4..].Reverse();
        reorderedOutput.CopyTo(output);
    }
}
