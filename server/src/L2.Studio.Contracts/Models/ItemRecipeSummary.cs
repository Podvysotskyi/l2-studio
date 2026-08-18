namespace L2.Studio.Contracts;

public sealed record ItemRecipeSummary(
    int Id,
    string Name,
    string ItemRecipeTypeName,
    int CraftLevel,
    int SuccessRate,
    ItemRecipeStatUseSummary? StatUse,
    IReadOnlyList<ItemRecipeItemSummary> Ingredients,
    IReadOnlyList<ItemRecipeItemSummary> Productions);
