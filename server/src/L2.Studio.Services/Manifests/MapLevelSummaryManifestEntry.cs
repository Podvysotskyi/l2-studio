namespace L2.Studio.Services;

internal sealed record MapLevelSummaryManifestEntry(
    string? Title,
    string? Author,
    string? Description,
    string? LevelEnterText,
    string? ExtraInfo,
    string? DecoTextName,
    bool? HideFromMenus,
    int? IdealPlayerCountMin,
    int? IdealPlayerCountMax,
    int? SinglePlayerTeamSize,
    string? Screenshot);
