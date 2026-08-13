namespace L2.Tools.StaticMeshConverter;

public sealed record StaticMeshMaterialFade(
    StaticMeshMaterialTint Color1,
    StaticMeshMaterialTint Color2,
    byte Type,
    float Period,
    float Phase);
