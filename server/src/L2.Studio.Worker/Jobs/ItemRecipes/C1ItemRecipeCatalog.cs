namespace L2.Studio.Worker;

public sealed partial class C1ItemRecipeCatalog
{
    public IReadOnlyList<ItemRecipeTypeDefinition> Types => TypeDefinitions;
    public IReadOnlyList<ItemRecipeDefinition> Recipes => Definitions;
}
