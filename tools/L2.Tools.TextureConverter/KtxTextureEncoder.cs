using System.Buffers.Binary;
using L2.Tools.PackageReader;

namespace L2.Tools.TextureConverter;

public static class KtxTextureEncoder
{
    private const uint Endianness = 0x04030201;
    private const uint GlRgba = 0x1908;
    private const uint GlCompressedRgbaS3tcDxt1 = 0x83f1;
    private const uint GlCompressedRgbaS3tcDxt3 = 0x83f2;
    private const uint GlCompressedRgbaS3tcDxt5 = 0x83f3;
    private static readonly byte[] Identifier =
        [0xab, 0x4b, 0x54, 0x58, 0x20, 0x31, 0x31, 0xbb, 0x0d, 0x0a, 0x1a, 0x0a];

    public static bool CanEncode(UnrealTexture texture) => texture.Format is
        UnrealTextureFormat.Dxt1 or UnrealTextureFormat.Dxt3 or UnrealTextureFormat.Dxt5;

    public static byte[] Encode(UnrealTexture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (!CanEncode(texture))
        {
            throw new InvalidDataException(
                $"Texture '{texture.Name}' uses {texture.Format}, which cannot be preserved as a DXT KTX texture.");
        }
        if (texture.Width <= 0 || texture.Height <= 0 || texture.MipLevels.Count == 0)
        {
            throw new InvalidDataException($"Texture '{texture.Name}' has no valid mip levels.");
        }

        using var output = new MemoryStream();
        output.Write(Identifier);
        WriteUInt32(output, Endianness);
        WriteUInt32(output, 0); // glType: compressed data
        WriteUInt32(output, 1); // glTypeSize
        WriteUInt32(output, 0); // glFormat: compressed data
        WriteUInt32(output, InternalFormat(texture.Format));
        WriteUInt32(output, GlRgba);
        WriteUInt32(output, checked((uint)texture.Width));
        WriteUInt32(output, checked((uint)texture.Height));
        WriteUInt32(output, 0); // pixelDepth
        WriteUInt32(output, 0); // array elements
        WriteUInt32(output, 1); // faces
        WriteUInt32(output, checked((uint)texture.MipLevels.Count));
        WriteUInt32(output, 0); // key/value bytes

        for (var index = 0; index < texture.MipLevels.Count; index++)
        {
            var mip = texture.MipLevels[index];
            var expectedWidth = Math.Max(1, texture.Width >> index);
            var expectedHeight = Math.Max(1, texture.Height >> index);
            if (mip.Width != expectedWidth || mip.Height != expectedHeight)
            {
                throw new InvalidDataException(
                    $"Texture '{texture.Name}' mip {index} has dimensions {mip.Width}x{mip.Height}; " +
                    $"{expectedWidth}x{expectedHeight} were expected.");
            }

            var byteCount = EncodedByteCount(texture.Format, mip.Width, mip.Height);
            if (mip.Data.Length < byteCount)
            {
                throw new InvalidDataException(
                    $"Texture '{texture.Name}' mip {index} has {mip.Data.Length} bytes; {byteCount} are required.");
            }

            WriteUInt32(output, checked((uint)byteCount));
            output.Write(mip.Data, 0, byteCount);
            while (output.Position % 4 != 0)
            {
                output.WriteByte(0);
            }
        }

        return output.ToArray();
    }

    private static uint InternalFormat(UnrealTextureFormat format) => format switch
    {
        UnrealTextureFormat.Dxt1 => GlCompressedRgbaS3tcDxt1,
        UnrealTextureFormat.Dxt3 => GlCompressedRgbaS3tcDxt3,
        UnrealTextureFormat.Dxt5 => GlCompressedRgbaS3tcDxt5,
        _ => throw new InvalidDataException($"Unsupported KTX texture format {format}.")
    };

    private static int EncodedByteCount(UnrealTextureFormat format, int width, int height)
    {
        var blockSize = format == UnrealTextureFormat.Dxt1 ? 8 : 16;
        return checked(((width + 3) / 4) * ((height + 3) / 4) * blockSize);
    }

    private static void WriteUInt32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(bytes, value);
        stream.Write(bytes);
    }
}
