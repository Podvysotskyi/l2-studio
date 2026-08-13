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
    float MaxFrameRate = 0,
    bool Masked = false,
    bool AlphaTexture = false,
    bool TwoSided = false,
    UnrealObjectReference? Detail = null,
    float DetailScale = 8,
    byte UClampMode = 0,
    byte VClampMode = 0);
