using L2.Tools.PackageReader;
using SixLabors.ImageSharp.PixelFormats;

namespace L2.Tools.TextureConverter;

public sealed record PackedTerrainControlMap(
    int Width,
    int Height,
    IReadOnlyList<int> LayerIndices,
    IReadOnlyList<Rgba32> Pixels);
