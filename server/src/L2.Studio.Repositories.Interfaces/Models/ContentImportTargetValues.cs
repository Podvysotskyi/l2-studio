namespace L2.Studio.Repositories.Interfaces.Models;

public static class ContentImportTargetValues
{
    public const string Items = "items";
    public const string ItemTypes = "item-types";
    public const string ItemActions = "item-actions";
    public const string ItemBodyParts = "item-body-parts";
    public const string ItemMaterials = "item-materials";
    public const string ItemCrystalTypes = "item-crystal-types";
    public const string ItemHandlers = "item-handlers";
    public const string ItemSkillTypes = "item-skill-types";
    public const string ItemSets = "item-sets";
    public const string ItemRecipes = "item-recipes";
    public const string Npcs = "npcs";
    public const string NpcTypes = "npc-types";
    public const string NpcRaces = "npc-races";
    public const string NpcSexes = "npc-sexes";
    public const string PlayerRaces = "player-races";
    public const string PlayerSexes = "player-sexes";
    public const string PlayerClasses = "player-classes";
    public const string PlayerFaces = "player-faces";
    public const string PlayerHairStyles = "player-hair-styles";
    public const string PlayerHairColors = "player-hair-colors";
    public const string Skills = "skills";
    public const string SkillOperateTypes = "skill-operate-types";
    public const string SkillTargetTypes = "skill-target-types";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Items, ItemTypes, ItemActions, ItemBodyParts, ItemMaterials, ItemCrystalTypes, ItemHandlers, ItemSkillTypes, ItemSets, ItemRecipes,
        Npcs, NpcTypes, NpcRaces, NpcSexes,
        PlayerRaces, PlayerSexes, PlayerClasses, PlayerFaces, PlayerHairStyles, PlayerHairColors,
        Skills, SkillOperateTypes, SkillTargetTypes
    };

    public static bool Supports(string gameVersion, string target) => All.Contains(target) &&
        (gameVersion == "c1" || target is NpcTypes or NpcRaces or NpcSexes &&
            gameVersion is "c4" or "interlude");

    public static string Family(string target) => target switch
    {
        Items or ItemTypes or ItemActions or ItemBodyParts or ItemMaterials or ItemCrystalTypes or ItemHandlers or ItemSkillTypes or ItemSets or ItemRecipes => "items",
        Npcs or NpcTypes or NpcRaces or NpcSexes => "npcs",
        PlayerRaces or PlayerSexes or PlayerClasses or PlayerFaces or PlayerHairStyles or PlayerHairColors => "players",
        Skills or SkillOperateTypes or SkillTargetTypes => "skills",
        _ => throw new ArgumentOutOfRangeException(nameof(target))
    };
}
