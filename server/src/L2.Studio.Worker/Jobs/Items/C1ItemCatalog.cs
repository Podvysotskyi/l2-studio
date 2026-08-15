namespace L2.Studio.Worker;

public sealed partial class C1ItemCatalog
{
    public IReadOnlyList<ItemDefinition> Items => Definitions;
    public IReadOnlyList<string> Types => Definitions.Select(item => item.TypeName).Distinct(StringComparer.Ordinal).Order().ToArray();
    public IReadOnlyList<string> Actions => Names(item => item.ActionName);
    public IReadOnlyList<string> BodyParts => Names(item => item.BodyPartName);
    public IReadOnlyList<string> Materials => Names(item => item.MaterialName);
    public IReadOnlyList<string> CrystalTypes => Names(item => item.CrystalTypeName);

    private static IReadOnlyList<string> Names(Func<ItemDefinition, string?> selector) =>
        Definitions.Select(selector).Where(value => value is not null).Select(value => value!).Distinct(StringComparer.Ordinal).Order().ToArray();
}
