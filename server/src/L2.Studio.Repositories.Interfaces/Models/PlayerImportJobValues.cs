namespace L2.Studio.Repositories.Interfaces.Models;

public static class PlayerImportJobValues
{
    public const string AddMissing = "add_missing";
    public const string RestoreDefaults = "restore_defaults";
    public const string Queued = "queued";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string Failed = "failed";

    public static readonly IReadOnlySet<string> SupportedModes = new HashSet<string>(StringComparer.Ordinal)
    {
        AddMissing, RestoreDefaults
    };
    public static readonly IReadOnlySet<string> ActiveStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        Queued, Running
    };
    public static readonly IReadOnlySet<string> TerminalStatuses = new HashSet<string>(StringComparer.Ordinal)
    {
        Succeeded, Failed
    };
}
