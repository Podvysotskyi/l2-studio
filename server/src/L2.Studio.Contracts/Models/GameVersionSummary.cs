namespace L2.Studio.Contracts;

public sealed record GameVersionSummary(
    string Key,
    string DisplayName,
    string SourceFolder,
    int SortOrder,
    bool IsDefault);
