namespace L2.Studio.Services.Interfaces;

public sealed class AssetImportOptions
{
    public const string SectionName = "AssetImport";

    public string SourceRootPath { get; init; } = "../../sources";
    public string AssetRootPath { get; init; } = "../../assets";
    public string StudioBaseUrl { get; init; } = "http://localhost:3001";
    public string LevelPreviewBrowserUrl { get; init; } = "http://localhost:9222";
}
