namespace L2.Studio.Services;

internal sealed record AnimationManifest(
    int SchemaVersion,
    string Kind,
    string SourceFolder,
    string SourceHash,
    int Protocol,
    IReadOnlyList<AnimationManifestPackage> Packages,
    IReadOnlyList<AnimationMeshManifestEntry> Meshes,
    IReadOnlyList<AnimationSetManifestEntry> AnimationSets);
