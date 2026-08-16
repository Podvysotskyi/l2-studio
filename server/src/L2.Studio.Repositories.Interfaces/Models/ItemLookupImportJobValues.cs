namespace L2.Studio.Repositories.Interfaces.Models;

public static class ItemLookupImportJobValues
{
    public const string ItemTypes = "item-types";
    public const string ItemActions = "item-actions";
    public const string ItemBodyParts = "item-body-parts";
    public const string ItemMaterials = "item-materials";
    public const string ItemCrystalTypes = "item-crystal-types";
    public const string ItemHandlers = "item-handlers";
    public const string ItemSkillTypes = "item-skill-types";
    public const string AddMissing = "add_missing";
    public const string RestoreDefaults = "restore_defaults";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    public static readonly IReadOnlySet<string> SupportedKinds = new HashSet<string>(StringComparer.Ordinal)
    {
        ItemTypes, ItemActions, ItemBodyParts, ItemMaterials, ItemCrystalTypes, ItemHandlers, ItemSkillTypes
    };
    public static readonly IReadOnlySet<string> SupportedModes = new HashSet<string>(StringComparer.Ordinal)
    {
        AddMissing, RestoreDefaults
    };
    public static readonly string[] ActiveStatuses = [Queued, Running];
    public static readonly IReadOnlySet<string> TerminalStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        Succeeded, Failed
    };

    public static bool Supports(string gameVersion, string kind) =>
        gameVersion == "c1" && SupportedKinds.Contains(kind);
}
