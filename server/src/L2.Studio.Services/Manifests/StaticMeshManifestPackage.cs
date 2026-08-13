namespace L2.Studio.Services;

internal sealed record StaticMeshManifestPackage(
    string Name,
    string FileName,
    string Sha256,
    int MeshCount);
