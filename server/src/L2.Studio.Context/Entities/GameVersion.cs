namespace L2.Studio.Context.Entities;

public sealed class GameVersion
{
    public string Key { get; set; } = string.Empty;
    public required string DisplayName { get; set; }
    public required string SourceFolder { get; set; }
    public int SortOrder { get; set; }
}
