using System.Text.RegularExpressions;

namespace L2.Studio.Worker;

public sealed class C1NpcLookupCatalog() : NpcLookupCatalog(
    typeNames:
    [
        "Artefact", "Auctioneer", "CastleDoorman", "ClanHallDoorman", "ClanHallManager",
        "ControlTower", "Defender", "EventMonster", "FlameTower", "Folk", "FriendlyMob",
        "GrandBoss", "Guard", "Merchant", "Monster", "Pet", "PetManager", "SchemeBuffer",
        "Servitor", "Teleporter", "Trainer", "VillageMasterDElf", "VillageMasterDwarf",
        "VillageMasterFighter", "VillageMasterMystic", "VillageMasterOrc",
        "VillageMasterPriest", "Warehouse"
    ],
    raceNames:
    [
        "HUMAN", "ELF", "DARK_ELF", "ORC", "DWARF", "ANIMAL", "BEAST", "BUG",
        "CASTLE_GUARD", "CONSTRUCT", "DEMONIC", "DRAGON", "ELEMENTAL", "ETC", "FAIRY",
        "GIANT", "HUMANOID", "MERCENARY", "PLANT", "SIEGE_WEAPON", "UNDEAD"
    ]);
