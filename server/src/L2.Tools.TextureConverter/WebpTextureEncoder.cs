using L2.Tools.PackageReader;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace L2.Tools.TextureConverter;

public static class WebpTextureEncoder
{
    public static async Task<byte[]> EncodeLosslessAsync(
        UnrealTexture texture,
        CancellationToken cancellationToken = default)
    {
        var pixels = DxtDecoder.Decode(texture);
        return await EncodeRgbaLosslessAsync(pixels, texture.Width, texture.Height, cancellationToken);
    }

    public static async Task<byte[]> EncodeRgbaLosslessAsync(
        ReadOnlyMemory<Rgba32> pixels,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        return await EncodeRgbaLosslessAsync(
            pixels,
            width,
            height,
            WebpTransparentColorMode.Clear,
            cancellationToken);
    }

    public static async Task<byte[]> EncodeRgbaDataLosslessAsync(
        ReadOnlyMemory<Rgba32> pixels,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        return await EncodeRgbaLosslessAsync(
            pixels,
            width,
            height,
            WebpTransparentColorMode.Preserve,
            cancellationToken);
    }

    private static async Task<byte[]> EncodeRgbaLosslessAsync(
        ReadOnlyMemory<Rgba32> pixels,
        int width,
        int height,
        WebpTransparentColorMode transparentColorMode,
        CancellationToken cancellationToken)
    {
        using var image = Image.LoadPixelData(pixels.Span, width, height);
        await using var output = new MemoryStream();
        await image.SaveAsWebpAsync(output, new WebpEncoder
        {
            FileFormat = WebpFileFormatType.Lossless,
            Method = WebpEncodingMethod.Level4,
            TransparentColorMode = transparentColorMode
        }, cancellationToken);
        return output.ToArray();
    }
}
