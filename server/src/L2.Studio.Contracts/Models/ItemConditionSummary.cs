namespace L2.Studio.Contracts;

public sealed record ItemConditionSummary(
    int MessageId,
    bool AddName,
    bool? IsPvpFlagged,
    IReadOnlyList<string> PlayerRaces,
    IReadOnlyList<string> PlayerCategoryTypes)
{
    public ItemConditionSummary(
        int messageId,
        bool addName,
        bool? isPvpFlagged,
        string? playerRaces,
        string? playerCategoryTypes)
        : this(
            messageId,
            addName,
            isPvpFlagged,
            SplitTokens(playerRaces),
            SplitTokens(playerCategoryTypes))
    {
    }

    private static IReadOnlyList<string> SplitTokens(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
