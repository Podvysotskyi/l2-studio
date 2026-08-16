namespace L2.Studio.Contracts;

public sealed record ItemTypeSummary(
    string Name,
    string DisplayName,
    string? ParentTypeName,
    string? ParentTypeDisplayName);
