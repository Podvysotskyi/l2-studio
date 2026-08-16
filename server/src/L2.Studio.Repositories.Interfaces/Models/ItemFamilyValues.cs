namespace L2.Studio.Repositories.Interfaces.Models;

public static class ItemFamilyValues
{
    public const string Armor = "armor";
    public const string Weapon = "weapon";
    public const string Arrow = "arrow";
    public const string Material = "material";
    public const string Potion = "potion";
    public const string Recipe = "recipe";
    public const string Enchant = "enchant";
    public const string Scroll = "scroll";
    public const string PetCollar = "pet-collar";
    public const string Etc = "etc";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Armor, Weapon, Arrow, Material, Potion, Recipe, Enchant, Scroll, PetCollar, Etc
    };

    public static readonly IReadOnlySet<string> SkillFamilies = new HashSet<string>(StringComparer.Ordinal)
    {
        Weapon, Potion, Enchant, Scroll, PetCollar, Etc
    };
}
