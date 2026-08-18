namespace L2.Studio.Worker;

public sealed record ItemRecipeDefinition(
    int Id,
    string Name,
    string ItemRecipeTypeName,
    int CraftLevel,
    int SuccessRate,
    IReadOnlyList<ItemRecipeIngredientDefinition> Ingredients,
    IReadOnlyList<ItemRecipeProductionDefinition> Productions,
    ItemRecipeStatUseDefinition StatUse);
