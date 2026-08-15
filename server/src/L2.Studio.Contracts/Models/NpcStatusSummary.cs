namespace L2.Studio.Contracts;

public sealed record NpcStatusSummary(
    bool Attackable,
    bool Targetable,
    bool Talkable,
    bool Undying,
    bool ShowName,
    bool RandomWalk,
    bool CanMove,
    bool NoSleepMode,
    bool CanBeSown);
