namespace L2.Studio.Repositories.Interfaces.Models;

public static class NpcLookupImportJobValues
{
    public const string NpcTypes = "npc-types";
    public const string NpcRaces = "npc-races";
    public const string NpcSexes = "npc-sexes";
    public const string AddMissing = "add_missing";
    public const string RestoreDefaults = "restore_defaults";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    public static readonly HashSet<string> SupportedKinds = [NpcTypes, NpcRaces, NpcSexes];
    public static readonly HashSet<string> SupportedModes = [AddMissing, RestoreDefaults];
    public static readonly HashSet<string> SupportedGameVersions = ["c1", "c4", "interlude"];
    public static readonly string[] ActiveStatuses = [Queued, Running];
    public static readonly string[] TerminalStatuses = [Succeeded, Failed];
}
