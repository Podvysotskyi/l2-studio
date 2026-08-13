namespace L2.Studio.Services.Interfaces;

public sealed class AssetImportOptions
{
    public const string SectionName = "AssetImport";

    public string SourceRootPath { get; init; } = "../../sources";
    public string AssetRootPath { get; init; } = "../../assets/public";
    public string AssetWorkRootPath { get; init; } = "../../assets/work";
    public string SourceSnapshotRootPath { get; init; } = "../../import-work";
    public string StudioBaseUrl { get; init; } = "http://localhost:3001";
    public string MapPreviewBrowserUrl { get; init; } = "http://localhost:9222";
    public string MapPreviewAssetBaseUrl { get; init; } = "http://localhost:5300";
    public TimeSpan AbandonedRunTimeout { get; init; } = TimeSpan.FromMinutes(15);
}
