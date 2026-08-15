namespace L2.Studio.Worker;

public sealed partial class C1ItemCatalog() : ItemLookupCatalog(
    TypeNames,
    ActionNames,
    BodyPartNames,
    MaterialNames,
    CrystalTypeNames)
{
    public IReadOnlyList<ItemDefinition> Items => Definitions;
}
