namespace L2.Studio.Contracts;

public sealed record AssetReleaseValidationIssue(string Code, string? Field, string Message);
