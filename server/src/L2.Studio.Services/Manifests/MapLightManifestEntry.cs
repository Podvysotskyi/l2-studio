namespace L2.Studio.Services;

internal sealed record MapLightManifestEntry(
    string Name,
    string ClassName,
    MapVector Location,
    MapRotation Rotation,
    float Brightness,
    byte Hue,
    byte Saturation,
    float Radius,
    IReadOnlyDictionary<string, string>? Properties = null,
    string? ResourceUrl = null);
