namespace L2.Tools.PackageReader;

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
