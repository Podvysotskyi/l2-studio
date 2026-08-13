using System.Text.RegularExpressions;

namespace L2.Studio.Worker;

public sealed class InterludeNpcLookupCatalog() : NpcLookupCatalog(
    typeNames:
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
    ],
    raceNames:
    [
        "HUMAN", "ELF", "DARK_ELF", "ORC", "DWARF", "ANIMAL", "BEAST", "BUG",
        "CASTLE_GUARD", "CONSTRUCT", "DEMONIC", "DIVINE", "DRAGON", "ELEMENTAL", "ETC",
        "FAIRY", "GIANT", "HUMANOID", "MERCENARY", "PLANT", "SIEGE_WEAPON", "UNDEAD"
    ]);
