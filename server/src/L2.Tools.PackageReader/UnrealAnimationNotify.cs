namespace L2.Tools.PackageReader;

public sealed record UnrealAnimationNotify(
    float NormalizedTime,
    string FunctionName,
    string? ObjectPath,
    string? ClassName,
    IReadOnlyDictionary<string, string> Properties);
