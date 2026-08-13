using L2.Tools.PackageReader;
using SixLabors.ImageSharp.PixelFormats;

namespace L2.Tools.TextureConverter;

public sealed record OpaqueTerrainControlMap(
    int Width,
    int Height,
    IReadOnlyList<Rgba32> Pixels);
