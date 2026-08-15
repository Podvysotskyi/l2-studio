using L2.Studio.Context.Identifiers;

namespace L2.Studio.Worker;

public sealed partial class C1PlayerCatalog
{
    private static readonly PlayerLookupDefinition<PlayerRaceId>[] RaceDefinitions =
    [
        new(PlayerRaceId.Human, "Human"), new(PlayerRaceId.Elf, "Elf"),
        new(PlayerRaceId.DarkElf, "Dark Elf"), new(PlayerRaceId.Orc, "Orc"),
        new(PlayerRaceId.Dwarf, "Dwarf")
    ];

    private static readonly PlayerLookupDefinition<PlayerSexId>[] SexDefinitions =
    [
        new(PlayerSexId.Male, "Male"), new(PlayerSexId.Female, "Female")
    ];

    public IReadOnlyList<PlayerLookupDefinition<PlayerRaceId>> Races => RaceDefinitions;
    public IReadOnlyList<PlayerLookupDefinition<PlayerSexId>> Sexes => SexDefinitions;
    public IReadOnlyList<PlayerClassDefinition> Classes => ClassDefinitions;
    public IReadOnlyList<PlayerAppearanceDefinition> Faces => Appearances(3, "Face");
    public IReadOnlyList<PlayerAppearanceDefinition> HairStyles =>
        [.. AppearanceDefinitions(Sexes, Races, "Hair style", sex => sex.Id == PlayerSexId.Female ? 7 : 5)];
    public IReadOnlyList<PlayerAppearanceDefinition> HairColors => Appearances(4, "Hair color");

    private static IReadOnlyList<PlayerAppearanceDefinition> Appearances(int count, string label) =>
        [.. AppearanceDefinitions(SexDefinitions, RaceDefinitions, label, _ => count)];

    private static IEnumerable<PlayerAppearanceDefinition> AppearanceDefinitions(
        IEnumerable<PlayerLookupDefinition<PlayerSexId>> sexes,
        IEnumerable<PlayerLookupDefinition<PlayerRaceId>> races,
        string label,
        Func<PlayerLookupDefinition<PlayerSexId>, int> count)
    {
        foreach (var sex in sexes)
        foreach (var race in races)
        for (var id = 0; id < count(sex); id++)
            yield return new PlayerAppearanceDefinition(id, sex.Id, race.Id, $"{label} {id}");
    }
}
