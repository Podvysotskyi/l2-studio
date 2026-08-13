namespace L2.Studio.Services;

internal sealed record ParticleSpriteSettings(
    string DirectionMode,
    string StartLocationShape,
    ParticleNumberRange SphereRadius,
    string RotationSource,
    int ColorScaleRepeats);
