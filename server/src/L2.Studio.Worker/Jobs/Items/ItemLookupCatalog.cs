using System.Text.RegularExpressions;

namespace L2.Studio.Worker;

public abstract partial class ItemLookupCatalog
{
    protected ItemLookupCatalog(
        IEnumerable<string> typeNames,
        IEnumerable<string> actionNames,
        IEnumerable<string> bodyPartNames,
        IEnumerable<string> materialNames,
        IEnumerable<string> crystalTypeNames,
        IEnumerable<string> handlerNames,
        IEnumerable<string> skillTypeNames)
    {
        Types = Definitions(typeNames);
        Actions = Definitions(actionNames);
        BodyParts = Definitions(bodyPartNames, new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["lhand"] = "Left Hand",
            ["rhand"] = "Right Hand",
            ["hands"] = "Two Hands"
        });
        Materials = Definitions(materialNames);
        CrystalTypes = Definitions(crystalTypeNames);
        Handlers = Definitions(handlerNames);
        SkillTypes = Definitions(skillTypeNames);
    }

    public IReadOnlyList<ItemLookupDefinition> Types { get; }
    public IReadOnlyList<ItemLookupDefinition> Actions { get; }
    public IReadOnlyList<ItemLookupDefinition> BodyParts { get; }
    public IReadOnlyList<ItemLookupDefinition> Materials { get; }
    public IReadOnlyList<ItemLookupDefinition> CrystalTypes { get; }
    public IReadOnlyList<ItemLookupDefinition> Handlers { get; }
    public IReadOnlyList<ItemLookupDefinition> SkillTypes { get; }

    public static string FriendlyName(string sourceName)
    {
        var words = PascalBoundary().Replace(sourceName.Replace('_', ' '), "$1 $2")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(Capitalize));
    }

    private static string Capitalize(string word)
    {
        var lower = word.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    private static IReadOnlyList<ItemLookupDefinition> Definitions(
        IEnumerable<string> names,
        IReadOnlyDictionary<string, string>? displayNames = null) =>
        names.Select(name => new ItemLookupDefinition(
            name,
            displayNames is not null && displayNames.TryGetValue(name, out var displayName)
                ? displayName
                : FriendlyName(name))).ToArray();

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex PascalBoundary();
}
