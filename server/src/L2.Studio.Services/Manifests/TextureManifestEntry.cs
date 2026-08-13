namespace L2.Studio.Services;

internal sealed record TextureManifestEntry(
    string PackageName,
    string ObjectName,
    string? Url,
    int Width,
    int Height,
    string Format,
    string? Sha256,
    string Status,
    string? Error,
    string? GpuUrl = null,
    string? GpuSha256 = null,
    bool GpuCompressed = false,
    int MipCount = 0,
    TextureAnimationManifestEntry? Animation = null,
    string OriginalFolder = "",
    string Path = "");
