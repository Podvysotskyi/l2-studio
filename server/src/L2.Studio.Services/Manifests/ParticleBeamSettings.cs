namespace L2.Studio.Services;

internal sealed record ParticleBeamSettings(
    string EndPointMode,
    IReadOnlyList<ParticleBeamEndPoint> EndPoints,
    float TextureUScale,
    float TextureVScale,
    int RotatingSheets);
