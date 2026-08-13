namespace L2.Studio.Repositories.Interfaces.Models;

public static class NpcLookupImportJobValues
{
    public const string NpcTypes = "npc-types";
    public const string NpcRaces = "npc-races";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    public static readonly HashSet<string> SupportedKinds = [NpcTypes, NpcRaces];
    public static readonly HashSet<string> SupportedGameVersions = ["c1", "c4", "interlude"];
    public static readonly string[] ActiveStatuses = [Queued, Running];
    public static readonly string[] TerminalStatuses = [Succeeded, Failed];
}
