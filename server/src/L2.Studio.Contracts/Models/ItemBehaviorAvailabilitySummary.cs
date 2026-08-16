namespace L2.Studio.Contracts;

public sealed record ItemBehaviorAvailabilitySummary(
    bool? EnchantEnabled,
    bool? ForNpc,
    bool? ImmediateEffect,
    bool? IsDepositable,
    bool? IsDestroyable,
    bool? IsDropable,
    bool? IsOlyRestricted,
    bool? IsSellable,
    bool? IsStackable,
    bool? IsTradable);
