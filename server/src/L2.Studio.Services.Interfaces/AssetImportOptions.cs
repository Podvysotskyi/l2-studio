namespace L2.Studio.Services.Interfaces;

public sealed class AssetImportOptions
{
    public const string SectionName = "AssetImport";

    public string SystemTexturesSourcePath { get; init; } = "../../sources/Interlude/systextures";
    public string TexturesSourcePath { get; init; } = "../../sources/Interlude/textures";
    public string MusicSourcePath { get; init; } = "../../sources/Interlude/music";
    public string SoundsSourcePath { get; init; } = "../../sources/Interlude/sounds";
    public string StaticMeshesSourcePath { get; init; } = "../../sources/Interlude/staticmeshes";
    public string LevelsSourcePath { get; init; } = "../../sources/Interlude/maps";
    public string AssetRootPath { get; init; } = "../../assets";
    public string StudioBaseUrl { get; init; } = "http://localhost:3001";
    public string LevelPreviewBrowserUrl { get; init; } = "http://localhost:9222";
}
