namespace L2.Studio.Services;

internal sealed record AnimationNotifyManifestEntry(
    float NormalizedTime,
    float TimeSeconds,
    string FunctionName,
    string? ObjectPath,
    string? ClassName,
    IReadOnlyDictionary<string, string> Properties);
