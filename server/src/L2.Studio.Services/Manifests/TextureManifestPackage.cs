namespace L2.Studio.Services;

internal sealed record TextureManifestPackage(
    string Name,
    string FileName,
    string Sha256,
    int TextureCount,
    int MaterialCount,
    string OriginalFolder = "",
    string Path = "");
