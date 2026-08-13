using L2.Tools.PackageReader;
using SixLabors.ImageSharp.PixelFormats;

namespace L2.Tools.TextureConverter;

public static class TerrainControlMapEncoder
{
    public const int ChannelsPerMap = 4;

    public static OpaqueTerrainControlMap EncodeOpaqueTransport(PackedTerrainControlMap controlMap)
    {
        ArgumentNullException.ThrowIfNull(controlMap);
        if (controlMap.Width <= 0 || controlMap.Height <= 0)
        {
            throw new InvalidDataException("Terrain control maps must have positive dimensions.");
        }
        if (controlMap.Pixels.Count != checked(controlMap.Width * controlMap.Height))
        {
            throw new InvalidDataException("Terrain control-map pixel count does not match its dimensions.");
        }

        var encodedWidth = checked(controlMap.Width * 2);
        var pixels = new Rgba32[checked(encodedWidth * controlMap.Height)];
        for (var y = 0; y < controlMap.Height; y++)
        {
            for (var x = 0; x < controlMap.Width; x++)
            {
                var source = controlMap.Pixels[y * controlMap.Width + x];
                var row = y * encodedWidth;
                pixels[row + x] = new Rgba32(source.R, source.G, source.B, 255);
                pixels[row + controlMap.Width + x] = new Rgba32(source.A, 0, 0, 255);
            }
        }

        return new OpaqueTerrainControlMap(encodedWidth, controlMap.Height, pixels);
    }

    public static IReadOnlyList<PackedTerrainControlMap> Pack(
        IReadOnlyList<UnrealTexture> alphaMaps)
    {
        ArgumentNullException.ThrowIfNull(alphaMaps);
        if (alphaMaps.Count == 0)
        {
            return [];
        }
        if (alphaMaps.Any(texture => texture.Width <= 0 || texture.Height <= 0))
        {
            throw new InvalidDataException("Terrain alpha maps must have positive dimensions.");
        }

        var width = alphaMaps.Max(texture => texture.Width);
        var height = alphaMaps.Max(texture => texture.Height);
        var decoded = alphaMaps.Select(DxtDecoder.Decode).ToArray();
        var results = new List<PackedTerrainControlMap>(
            (alphaMaps.Count + ChannelsPerMap - 1) / ChannelsPerMap);

        for (var firstLayer = 0; firstLayer < alphaMaps.Count; firstLayer += ChannelsPerMap)
        {
            var layerCount = Math.Min(ChannelsPerMap, alphaMaps.Count - firstLayer);
            var pixels = new Rgba32[checked(width * height)];
            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    byte red = 0;
                    byte green = 0;
                    byte blue = 0;
                    byte alpha = 0;
                    for (var channel = 0; channel < layerCount; channel++)
                    {
                        var layerIndex = firstLayer + channel;
                        var weight = SampleWeight(
                            alphaMaps[layerIndex],
                            decoded[layerIndex],
                            x,
                            y,
                            width,
                            height);
                        switch (channel)
                        {
                            case 0: red = weight; break;
                            case 1: green = weight; break;
                            case 2: blue = weight; break;
                            case 3: alpha = weight; break;
                        }
                    }
                    pixels[y * width + x] = new Rgba32(red, green, blue, alpha);
                }
            }
            results.Add(new PackedTerrainControlMap(
                width,
                height,
                Enumerable.Range(firstLayer, layerCount).ToArray(),
                pixels));
        }

        return results;
    }

    private static byte SampleWeight(
        UnrealTexture texture,
        IReadOnlyList<Rgba32> pixels,
        int targetX,
        int targetY,
        int targetWidth,
        int targetHeight)
    {
        var sourceX = ((targetX + 0.5f) * texture.Width / targetWidth) - 0.5f;
        var sourceY = ((targetY + 0.5f) * texture.Height / targetHeight) - 0.5f;
        var x0 = Math.Clamp((int)MathF.Floor(sourceX), 0, texture.Width - 1);
        var y0 = Math.Clamp((int)MathF.Floor(sourceY), 0, texture.Height - 1);
        var x1 = Math.Min(x0 + 1, texture.Width - 1);
        var y1 = Math.Min(y0 + 1, texture.Height - 1);
        var tx = Math.Clamp(sourceX - x0, 0, 1);
        var ty = Math.Clamp(sourceY - y0, 0, 1);
        var top = Lerp(
            Weight(texture, pixels[y0 * texture.Width + x0]),
            Weight(texture, pixels[y0 * texture.Width + x1]),
            tx);
        var bottom = Lerp(
            Weight(texture, pixels[y1 * texture.Width + x0]),
            Weight(texture, pixels[y1 * texture.Width + x1]),
            tx);
        return (byte)Math.Clamp(MathF.Round(Lerp(top, bottom, ty)), 0, 255);
    }

    private static byte Weight(UnrealTexture texture, Rgba32 pixel) => texture.Format switch
    {
        UnrealTextureFormat.Dxt3 or UnrealTextureFormat.Dxt5 or UnrealTextureFormat.Rgba8 => pixel.A,
        _ => pixel.R
    };

    private static float Lerp(float left, float right, float amount) =>
        left + (right - left) * amount;
}
