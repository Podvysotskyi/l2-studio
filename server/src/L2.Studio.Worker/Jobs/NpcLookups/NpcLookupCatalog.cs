using System.Text.RegularExpressions;

namespace L2.Studio.Worker;

public abstract partial class NpcLookupCatalog
{
    protected NpcLookupCatalog(
        IEnumerable<string> typeNames,
        IEnumerable<string> raceNames,
        IEnumerable<string>? sexNames = null)
    {
        Types = Definitions(typeNames);
        Races = Definitions(raceNames);
        Sexes = Definitions(sexNames ?? ["MALE", "FEMALE", "ETC"]);
    }

    public IReadOnlyList<NpcLookupDefinition> Types { get; }
    public IReadOnlyList<NpcLookupDefinition> Races { get; }
    public IReadOnlyList<NpcLookupDefinition> Sexes { get; }

    public static string FriendlyName(string sourceName)
    {
        if (sourceName == "VillageMasterDElf") return "Village Master Dark Elf";
        var words = PascalBoundary().Replace(sourceName.Replace('_', ' '), "$1 $2")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return string.Join(' ', words.Select(Capitalize));
    }

    private static string Capitalize(string word)
    {
        var lower = word.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    private static IReadOnlyList<NpcLookupDefinition> Definitions(IEnumerable<string> names) =>
        names.Select(name => new NpcLookupDefinition(name, FriendlyName(name))).ToArray();

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex PascalBoundary();
}
