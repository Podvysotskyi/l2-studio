namespace L2.Tools.PackageReader;

public enum UnrealTextureFormat : byte
{
    P8 = 0,
    Dxt1 = 3,
    Rgba8 = 5,
    Dxt3 = 7,
    Dxt5 = 8,
    G16 = 10
}

public readonly record struct UnrealColor(byte Red, byte Green, byte Blue, byte Alpha);

public sealed record UnrealTextureMip(int Width, int Height, byte[] Data);

public sealed record UnrealTexture(
    string Name,
    UnrealTextureFormat Format,
    int Width,
    int Height,
    byte[] Data,
    IReadOnlyList<UnrealColor>? Palette = null,
    IReadOnlyList<UnrealTextureMip>? Mips = null)
{
    public IReadOnlyList<UnrealTextureMip> MipLevels { get; } =
        Mips ?? [new UnrealTextureMip(Width, Height, Data)];
}

public sealed record UnrealTextureExport(
    string Name,
    byte? Format,
    int Width,
    int Height,
    UnrealTexture? Texture,
    int MipCount,
    UnrealObjectReference? AnimationNext = null,
    float MinFrameRate = 0,
    float MaxFrameRate = 0);
