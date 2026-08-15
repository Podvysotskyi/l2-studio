namespace L2.Studio.Repositories.Interfaces.Models;

public static class ImportJobValues
{
    public const string Content = "content";
    public const string Asset = "asset";
    public const string Queued = "queued";
    public const string Discovering = "discovering";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string SucceededWithWarnings = "succeeded_with_warnings";
    public const string Failed = "failed";
    public const string AddMissing = "add_missing";
    public const string RestoreDefaults = "restore_defaults";

    public static readonly string[] ActiveStatuses = [Queued, Discovering, Running];
    public static readonly string[] TerminalStatuses = [Succeeded, SucceededWithWarnings, Failed];
    public static readonly string[] ContentModes = [AddMissing, RestoreDefaults];
    public static readonly string[] Statuses = [.. ActiveStatuses, .. TerminalStatuses];
}
