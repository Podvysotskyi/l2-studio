namespace L2.Studio.Services;

internal sealed record NpcAppearanceCatalogMetadata(
    string NpcManifestUrlTemplate,
    IReadOnlyList<int> NpcIds,
    int NpcCount,
    int SourceAppearanceCount,
    int MatchedNpcCount,
    int UnmatchedNpcCount,
    int UnusedAppearanceCount,
    int ResolvedReferenceCount,
    int UnresolvedReferenceCount);
