namespace L2.Studio.Services;

internal sealed record NpcAppearanceManifest(
    int SchemaVersion,
    string Kind,
    string SourceKey,
    string SourceHash,
    int Protocol,
    NpcAppearanceManifestEntry Npc);
