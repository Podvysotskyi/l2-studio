namespace L2.Studio.Worker;

public sealed record NpcStatusDefinition(
    bool Attackable,
    bool Targetable,
    bool Talkable,
    bool Undying,
    bool ShowName,
    bool RandomWalk,
    bool CanMove,
    bool NoSleepMode,
    bool CanBeSown);
