using L2.Tools.PackageReader;
using SixLabors.ImageSharp.PixelFormats;

namespace L2.Tools.TextureConverter;

public static class DxtDecoder
{
    public static Rgba32[] Decode(UnrealTexture texture)
    {
        ArgumentNullException.ThrowIfNull(texture);
        if (texture.Width <= 0 || texture.Height <= 0 || texture.Width > 16384 || texture.Height > 16384)
        {
            throw new InvalidDataException($"Texture '{texture.Name}' has invalid dimensions {texture.Width}x{texture.Height}.");
        }

        return texture.Format switch
        {
            UnrealTextureFormat.P8 => DecodeP8(texture),
            UnrealTextureFormat.Rgba8 => DecodeRgba8(texture),
            UnrealTextureFormat.G16 => DecodeG16(texture),
            UnrealTextureFormat.Dxt1 or UnrealTextureFormat.Dxt3 or UnrealTextureFormat.Dxt5 => DecodeDxt(texture),
            _ => throw new InvalidDataException($"Texture '{texture.Name}' uses unsupported format {texture.Format}.")
        };
    }

    private static Rgba32[] DecodeDxt(UnrealTexture texture)
    {
        var pixels = new Rgba32[checked(texture.Width * texture.Height)];
        var blockSize = texture.Format == UnrealTextureFormat.Dxt1 ? 8 : 16;
        var blocksWide = (texture.Width + 3) / 4;
        var blocksHigh = (texture.Height + 3) / 4;
        var expected = checked(blocksWide * blocksHigh * blockSize);
        if (texture.Data.Length < expected)
        {
            throw new InvalidDataException($"Texture '{texture.Name}' has {texture.Data.Length} bytes; {expected} are required.");
        }

        var offset = 0;
        Span<byte> alpha = stackalloc byte[16];
        for (var blockY = 0; blockY < blocksHigh; blockY++)
        {
            for (var blockX = 0; blockX < blocksWide; blockX++)
            {
                alpha.Fill(255);
                if (texture.Format == UnrealTextureFormat.Dxt3)
                {
                    var encodedAlpha = BitConverter.ToUInt64(texture.Data, offset);
                    for (var pixel = 0; pixel < alpha.Length; pixel++)
                    {
                        alpha[pixel] = (byte)(((encodedAlpha >> (pixel * 4)) & 0xf) * 17);
                    }

                    offset += 8;
                }
                else if (texture.Format == UnrealTextureFormat.Dxt5)
                {
                    DecodeDxt5Alpha(texture.Data.AsSpan(offset, 8), alpha);
                    offset += 8;
                }

                var color0 = BitConverter.ToUInt16(texture.Data, offset);
                var color1 = BitConverter.ToUInt16(texture.Data, offset + 2);
                var indices = BitConverter.ToUInt32(texture.Data, offset + 4);
                offset += 8;
                var palette = BuildPalette(color0, color1, texture.Format == UnrealTextureFormat.Dxt1);

                for (var pixel = 0; pixel < 16; pixel++)
                {
                    var localX = pixel & 3;
                    var localY = pixel >> 2;
                    var x = blockX * 4 + localX;
                    var y = blockY * 4 + localY;
                    if (x >= texture.Width || y >= texture.Height)
                    {
                        continue;
                    }

                    var color = palette[(indices >> (pixel * 2)) & 3];
                    if (texture.Format is UnrealTextureFormat.Dxt3 or UnrealTextureFormat.Dxt5)
                    {
                        color.A = alpha[pixel];
                    }

                    pixels[y * texture.Width + x] = color;
                }
            }
        }

        return pixels;
    }

    private static Rgba32[] DecodeP8(UnrealTexture texture)
    {
        var palette = texture.Palette ?? throw new InvalidDataException($"Texture '{texture.Name}' has no palette.");
        var pixelCount = checked(texture.Width * texture.Height);
        if (texture.Data.Length < pixelCount)
        {
            throw new InvalidDataException($"Texture '{texture.Name}' has {texture.Data.Length} palette indices; {pixelCount} are required.");
        }

        var pixels = new Rgba32[pixelCount];
        for (var index = 0; index < pixels.Length; index++)
        {
            var paletteIndex = texture.Data[index];
            if (paletteIndex >= palette.Count)
            {
                throw new InvalidDataException($"Texture '{texture.Name}' references palette index {paletteIndex} outside its palette.");
            }

            var color = palette[paletteIndex];
            pixels[index] = new Rgba32(color.Red, color.Green, color.Blue, color.Alpha);
        }

        return pixels;
    }

    private static Rgba32[] DecodeRgba8(UnrealTexture texture)
    {
        var pixelCount = checked(texture.Width * texture.Height);
        var required = checked(pixelCount * 4);
        if (texture.Data.Length < required)
        {
            throw new InvalidDataException($"Texture '{texture.Name}' has {texture.Data.Length} color bytes; {required} are required.");
        }

        var pixels = new Rgba32[pixelCount];
        for (var index = 0; index < pixels.Length; index++)
        {
            var offset = index * 4;
            pixels[index] = new Rgba32(
                texture.Data[offset + 2],
                texture.Data[offset + 1],
                texture.Data[offset],
                texture.Data[offset + 3]);
        }

        return pixels;
    }

    private static Rgba32[] DecodeG16(UnrealTexture texture)
    {
        var pixelCount = checked(texture.Width * texture.Height);
        var required = checked(pixelCount * 2);
        if (texture.Data.Length < required)
        {
            throw new InvalidDataException($"Texture '{texture.Name}' has {texture.Data.Length} grayscale bytes; {required} are required.");
        }

        var pixels = new Rgba32[pixelCount];
        for (var index = 0; index < pixels.Length; index++)
        {
            var value = texture.Data[index * 2 + 1];
            pixels[index] = new Rgba32(value, value, value, 255);
        }

        return pixels;
    }

    private static void DecodeDxt5Alpha(ReadOnlySpan<byte> data, Span<byte> output)
    {
        var alpha0 = data[0];
        var alpha1 = data[1];
        Span<byte> palette = stackalloc byte[8];
        palette[0] = alpha0;
        palette[1] = alpha1;
        if (alpha0 > alpha1)
        {
            for (var index = 1; index <= 6; index++)
            {
                palette[index + 1] = (byte)(((7 - index) * alpha0 + index * alpha1) / 7);
            }
        }
        else
        {
            for (var index = 1; index <= 4; index++)
            {
                palette[index + 1] = (byte)(((5 - index) * alpha0 + index * alpha1) / 5);
            }

            palette[6] = 0;
            palette[7] = 255;
        }

        ulong indices = 0;
        for (var index = 0; index < 6; index++)
        {
            indices |= (ulong)data[index + 2] << (index * 8);
        }

        for (var pixel = 0; pixel < 16; pixel++)
        {
            output[pixel] = palette[(int)((indices >> (pixel * 3)) & 7)];
        }
    }

    private static Rgba32[] BuildPalette(ushort value0, ushort value1, bool allowTransparent)
    {
        var color0 = FromRgb565(value0);
        var color1 = FromRgb565(value1);
        var colors = new Rgba32[4];
        colors[0] = color0;
        colors[1] = color1;
        if (allowTransparent && value0 <= value1)
        {
            colors[2] = Mix(color0, color1, 1, 1, 2);
            colors[3] = new Rgba32(0, 0, 0, 0);
        }
        else
        {
            colors[2] = Mix(color0, color1, 2, 1, 3);
            colors[3] = Mix(color0, color1, 1, 2, 3);
        }

        return colors;
    }

    private static Rgba32 FromRgb565(ushort value)
    {
        var red = (value >> 11) & 0x1f;
        var green = (value >> 5) & 0x3f;
        var blue = value & 0x1f;
        return new Rgba32(
            (byte)((red << 3) | (red >> 2)),
            (byte)((green << 2) | (green >> 4)),
            (byte)((blue << 3) | (blue >> 2)),
            255);
    }

    private static Rgba32 Mix(Rgba32 first, Rgba32 second, int firstWeight, int secondWeight, int divisor) =>
        new(
            (byte)((first.R * firstWeight + second.R * secondWeight) / divisor),
            (byte)((first.G * firstWeight + second.G * secondWeight) / divisor),
            (byte)((first.B * firstWeight + second.B * secondWeight) / divisor),
            255);
}
