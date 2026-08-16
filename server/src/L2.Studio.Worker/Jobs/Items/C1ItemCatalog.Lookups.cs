namespace L2.Studio.Worker;

public sealed partial class C1ItemCatalog
{
    private static readonly ItemLookupDefinition[] TypeDefinitions =
    [
        new("Armor", "Armor"),
        new("EtcItem", "Etc Item"),
        new("Weapon", "Weapon"),
        new("HEAVY", "Heavy", "Armor"),
        new("LIGHT", "Light", "Armor"),
        new("MAGIC", "Magic", "Armor"),
        new("ARROW", "Arrow", "EtcItem"),
        new("CASTLE_GUARD", "Castle Guard", "EtcItem"),
        new("MATERIAL", "Material", "EtcItem"),
        new("PET_COLLAR", "Pet Collar", "EtcItem"),
        new("POTION", "Potion", "EtcItem"),
        new("RECIPE", "Recipe", "EtcItem"),
        new("SCRL_ENCHANT_AM", "Scrl Enchant Am", "EtcItem"),
        new("SCRL_ENCHANT_WP", "Scrl Enchant Wp", "EtcItem"),
        new("SCROLL", "Scroll", "EtcItem"),
        new("BLUNT", "Blunt", "Weapon"),
        new("BOW", "Bow", "Weapon"),
        new("DAGGER", "Dagger", "Weapon"),
        new("DUAL", "Dual", "Weapon"),
        new("DUALFIST", "Dualfist", "Weapon"),
        new("ETC", "Etc", "Weapon"),
        new("FIST", "Fist", "Weapon"),
        new("FLAG", "Flag", "Weapon"),
        new("POLE", "Pole", "Weapon"),
        new("SWORD", "Sword", "Weapon"),
    ];

    private static readonly string[] ActionNames =
    [
        "EQUIP",
        "RECIPE",
        "SKILL_MAINTAIN",
        "SKILL_REDUCE",
        "SOULSHOT",
        "SPIRITSHOT",
    ];

    private static readonly string[] BodyPartNames =
    [
        "back",
        "chest",
        "ear",
        "feet",
        "finger",
        "gloves",
        "hands",
        "head",
        "legs",
        "lhand",
        "neck",
        "onepiece",
        "rhand",
        "underwear",
    ];

    private static readonly string[] MaterialNames =
    [
        "ADAMANTAITE",
        "BLOOD_STEEL",
        "BONE",
        "BRONZE",
        "CHRYSOLITE",
        "CLOTH",
        "COBWEB",
        "COTTON",
        "CRYSTAL",
        "DAMASCUS",
        "DYESTUFF",
        "FINE_STEEL",
        "GOLD",
        "HORN",
        "LEATHER",
        "LIQUID",
        "MITHRIL",
        "ORIHARUKON",
        "PAPER",
        "SCALE_OF_DRAGON",
        "SILVER",
        "STEEL",
        "WOOD",
    ];

    private static readonly string[] CrystalTypeNames =
    [
        "A",
        "B",
        "C",
        "D",
        "S",
    ];

    private static readonly string[] HandlerNames =
    [
        "BlessedSpiritShot",
        "EnchantScrolls",
        "ItemSkills",
        "Maps",
        "MercTicket",
        "PetFood",
        "Recipes",
        "SoulShots",
        "SpiritShot",
        "SummonItems",
    ];

    private static readonly string[] SkillTypeNames =
    [
        "ON_CRITICAL_SKILL",
        "ON_ENCHANT_4",
    ];

}
