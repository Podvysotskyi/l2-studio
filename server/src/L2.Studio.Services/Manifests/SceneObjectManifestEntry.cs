namespace L2.Studio.Services;

internal sealed record SceneObjectManifestEntry(
    int Order,
    string Name,
    string ClassName,
    MapVector Location,
    MapRotation Rotation,
    float Duration,
    string? Target,
    IReadOnlyDictionary<string, string> Properties,
    string? Owner = null,
    string? ResourceUrl = null,
    ParticleEmitterManifestEntry? Particle = null,
    string? Diagnostic = null);
