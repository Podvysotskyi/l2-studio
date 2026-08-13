using System.Text.RegularExpressions;

namespace L2.Studio.Services;

public sealed record NpcLookupDefinition(string Name, string DisplayName);

public static partial class NpcLookupCatalogs
{
    private static readonly string[] AllTypeNames =
    [
        "Adventurer", "Artefact", "Auctioneer", "BabyPet", "BroadcastingTower",
        "CastleDoorman", "Chest", "ClanHallDoorman", "ClanHallManager", "ControlTower",
        "DawnPriest", "Defender", "Doorman", "DungeonGatekeeper", "DuskPriest",
        "EffectPoint", "EventMonster", "FeedableBeast", "FestivalGuide", "FestivalMonster",
        "Fisherman", "FlameTower", "FlyTerrainObject", "Folk", "FriendlyMob", "GrandBoss",
        "Guard", "Merchant", "Monster", "OlympiadManager", "Pet", "PetManager", "RaceManager",
        "RaidBoss", "RiftInvader", "SchemeBuffer", "Servitor", "SignsPriest", "TamedBeast",
        "Teleporter", "Trainer", "VillageMasterDElf", "VillageMasterDwarf",
        "VillageMasterFighter", "VillageMasterMystic", "VillageMasterOrc",
        "VillageMasterPriest", "Warehouse"
    ];

    private static readonly string[] C1TypeNames =
    [
        "Artefact", "Auctioneer", "CastleDoorman", "ClanHallDoorman", "ClanHallManager",
        "ControlTower", "Defender", "EventMonster", "FlameTower", "Folk", "FriendlyMob",
        "GrandBoss", "Guard", "Merchant", "Monster", "Pet", "PetManager", "SchemeBuffer",
        "Servitor", "Teleporter", "Trainer", "VillageMasterDElf", "VillageMasterDwarf",
        "VillageMasterFighter", "VillageMasterMystic", "VillageMasterOrc",
        "VillageMasterPriest", "Warehouse"
    ];

    private static readonly string[] AllRaceNames =
    [
        "HUMAN", "ELF", "DARK_ELF", "ORC", "DWARF", "ANIMAL", "BEAST", "BUG",
        "CASTLE_GUARD", "CONSTRUCT", "DEMONIC", "DIVINE", "DRAGON", "ELEMENTAL", "ETC",
        "FAIRY", "GIANT", "HUMANOID", "MERCENARY", "PLANT", "SIEGE_WEAPON", "UNDEAD"
    ];

    public static readonly IReadOnlyList<NpcLookupDefinition> C1Types = Definitions(C1TypeNames);
    public static readonly IReadOnlyList<NpcLookupDefinition> C4Types = Definitions(
        AllTypeNames.Where(name => name is not "EffectPoint" and not "FlyTerrainObject"));
    public static readonly IReadOnlyList<NpcLookupDefinition> InterludeTypes = Definitions(AllTypeNames);
    public static readonly IReadOnlyList<NpcLookupDefinition> C1Races = Definitions(
        AllRaceNames.Where(name => name != "DIVINE"));
    public static readonly IReadOnlyList<NpcLookupDefinition> C4Races = Definitions(AllRaceNames);
    public static readonly IReadOnlyList<NpcLookupDefinition> InterludeRaces = Definitions(AllRaceNames);

    public static string FriendlyName(string sourceName)
    {
        if (sourceName == "VillageMasterDElf") return "Village Master Dark Elf";
        if (sourceName.Contains('_', StringComparison.Ordinal))
        {
            return string.Join(' ', sourceName.Split('_').Select(word =>
                string.Concat(char.ToUpperInvariant(word[0]), word.AsSpan(1).ToString().ToLowerInvariant())));
        }

        return PascalBoundary().Replace(sourceName, "$1 $2");
    }

    private static IReadOnlyList<NpcLookupDefinition> Definitions(IEnumerable<string> names) =>
        names.Select(name => new NpcLookupDefinition(name, FriendlyName(name))).ToArray();

    [GeneratedRegex("([a-z0-9])([A-Z])", RegexOptions.CultureInvariant)]
    private static partial Regex PascalBoundary();
}
