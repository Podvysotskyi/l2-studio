namespace L2.Tools.PackageReader;

public sealed record UnrealLevelSummary(
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
    UnrealObjectReference? Screenshot);
