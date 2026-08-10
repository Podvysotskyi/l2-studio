using System.Text;

namespace L2.Tools.PackageReader;

public static class LineagePackageDecoder
{
    private const int HeaderSize = 28;
    private const int TailSize = 20;

    public static byte[] DecodeProtocol111(ReadOnlySpan<byte> input) =>
        DecodeXorPackage(input, "Lineage2Ver111", 0xac, "Protocol 111");

    public static byte[] DecodeProtocol121(ReadOnlySpan<byte> input, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var key = Encoding.ASCII.GetBytes(fileName.ToLowerInvariant()).Sum(value => value) & 0xff;
        return DecodeXorPackage(input, "Lineage2Ver121", (byte)key, "Protocol 121");
    }

    private static byte[] DecodeXorPackage(
        ReadOnlySpan<byte> input,
        string expectedHeader,
        byte key,
        string protocolName)
    {
        if (input.Length < HeaderSize + TailSize)
        {
            throw new InvalidDataException("The Lineage package is shorter than its protocol header and tail.");
        }

        var header = Encoding.Unicode.GetString(input[..HeaderSize]);
        if (!string.Equals(header, expectedHeader, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"Unsupported Lineage package header '{header}'.");
        }

        var decoded = input[HeaderSize..^TailSize].ToArray();
        for (var index = 0; index < decoded.Length; index++)
        {
            decoded[index] ^= key;
        }

        if (decoded.Length < sizeof(uint) || BitConverter.ToUInt32(decoded) != UnrealPackageReader.PackageTag)
        {
            throw new InvalidDataException($"{protocolName} decoding did not produce an Unreal package.");
        }

        return decoded;
    }
}
