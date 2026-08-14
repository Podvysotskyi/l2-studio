namespace L2.Studio.Repositories.Interfaces.Models;

public static class AssetImportJobValues
{
    public const string Textures = "textures";
    public const string Music = "music";
    public const string Sounds = "sounds";
    public const string StaticMeshes = "staticmeshes";
    public const string Animations = "animations";
    public const string Maps = "maps";
    public const string MapPreviews = "mappreviews";
    public const string Scenes = "scenes";
    public const string Queued = "queued";
    public const string Discovering = "discovering";
    public const string Running = "running";
    public const string Succeeded = "succeeded";
    public const string SucceededWithWarnings = "succeeded_with_warnings";
    public const string Failed = "failed";
    public const string Reused = "reused";

    public const string FullScan = "full_scan";
    public const string SingleFile = "single_file";
    public const string StaleRebuild = "stale_rebuild";
    public static readonly string[] ActiveStatuses = [Queued, Discovering, Running];
    public static readonly string[] TerminalStatuses = [Succeeded, SucceededWithWarnings, Failed];
    public static readonly string[] WorkItemTerminalStatuses = [Succeeded, SucceededWithWarnings, Failed, Reused];
    public static readonly HashSet<string> SupportedKinds =
    [
        Textures, Music, Sounds, StaticMeshes, Animations, Maps, MapPreviews, Scenes
    ];
}
