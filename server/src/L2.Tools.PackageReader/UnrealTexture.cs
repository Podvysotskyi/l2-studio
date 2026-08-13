namespace L2.Tools.PackageReader;

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
