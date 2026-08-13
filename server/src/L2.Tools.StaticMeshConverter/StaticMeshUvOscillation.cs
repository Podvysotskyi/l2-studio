namespace L2.Tools.StaticMeshConverter;

public sealed record StaticMeshUvOscillation(
    byte UType,
    byte VType,
    float URate,
    float VRate,
    float UAmplitude,
    float VAmplitude,
    float UPhase,
    float VPhase);
