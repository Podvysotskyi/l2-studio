using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Tools.AudioConverter;
using L2.Tools.PackageReader;
using L2.Tools.TextureConverter;
using L2.Tools.StaticMeshConverter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using PuppeteerSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace L2.Studio.Services;

public sealed partial class AssetImportJobProcessor
{
    private async Task ImportMapsAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken) =>
        await ImportUnrealPackagesAsync(context, job, scenes: false, cancellationToken);

    private async Task ImportScenesAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken) =>
        await ImportUnrealPackagesAsync(context, job, scenes: true, cancellationToken);

    private async Task ImportMapPreviewsAsync(
        GameContentDbContext context,
        AssetImportJob job,
        CancellationToken cancellationToken)
    {
        var assetRootPath = AssetRoot(job);
        var mapCatalogRecord = await context.AssetCatalogs.AsNoTracking().AsSplitQuery().Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.GameVersion == job.GameVersion &&
                item.Kind == AssetImportJobValues.Maps && item.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Generate the map catalog before generating map previews.");
        var mapCatalog = new MapCatalogManifest(
            mapCatalogRecord.SchemaVersion, mapCatalogRecord.Kind, mapCatalogRecord.SourceFolder,
            mapCatalogRecord.SourceHash, mapCatalogRecord.Protocol ?? 0,
            mapCatalogRecord.Items.Select(item => JsonSerializer.Deserialize<MapCatalogEntry>(item.MetadataJson, ManifestJsonOptions)!).ToArray());
        var allMaps = mapCatalog.Maps.OrderBy(map => map.Name, StringComparer.OrdinalIgnoreCase).ToArray();
        var requestedMapName = MapPreviewGeneration.RequestedMapName(
            SourceRoot(job, AssetImportJobValues.Maps),
            job.SourcePath);
        if (requestedMapName is not null && !allMaps.Any(map =>
                string.Equals(map.Name, requestedMapName, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                $"The requested map '{requestedMapName}' does not exist in the active map catalog.");
        }
        var maps = requestedMapName is null
            ? allMaps
            : allMaps.Where(map => string.Equals(map.Name, requestedMapName, StringComparison.OrdinalIgnoreCase)).ToArray();
        job.TotalCount = maps.Length;
        await context.SaveChangesAsync(cancellationToken);

        var (finalPath, stagingPath, outputUrlRoot) = OutputPaths(assetRootPath, job);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        Directory.CreateDirectory(stagingPath);
        var warnings = new List<string>();
        var entries = new Dictionary<string, MapPreviewCatalogEntry>(StringComparer.OrdinalIgnoreCase);
        var changed = new List<MapPreviewRenderMap>();
        var previous = await ReadMapPreviewCatalogAsync(context, job.GameVersion, cancellationToken);

        try
        {
            foreach (var map in maps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                RequireSafeSegment(map.Name, "map name");
                var old = previous?.Previews.FirstOrDefault(item =>
                    string.Equals(item.Name, map.Name, StringComparison.OrdinalIgnoreCase));
                var oldImagePath = Path.Combine(finalPath, $"{map.Name}.webp");
                var isRequestedMap = requestedMapName is null || string.Equals(
                    map.Name,
                    requestedMapName,
                    StringComparison.OrdinalIgnoreCase);
                if (!isRequestedMap)
                {
                    var oldImageExists = File.Exists(oldImagePath);
                    if (MapPreviewGeneration.CanCarryForward(old, oldImageExists) && old!.Status == "resolved")
                    {
                        File.Copy(oldImagePath, Path.Combine(stagingPath, $"{map.Name}.webp"));
                        entries[map.Name] = old;
                    }
                    else if (MapPreviewGeneration.CanCarryForward(old, oldImageExists))
                    {
                        entries[map.Name] = old!;
                    }
                    else
                    {
                        entries[map.Name] = new MapPreviewCatalogEntry(
                            map.Name, map.Sha256, null, MapPreviewGeneration.Size, MapPreviewGeneration.Size,
                            "skipped", old is null
                                ? "Preview has not been generated."
                                : "The previous preview image is missing.");
                    }
                    continue;
                }
                if (map.Status != "resolved" || map.ManifestUrl is null)
                {
                    entries[map.Name] = new MapPreviewCatalogEntry(
                        map.Name, map.Sha256, null, MapPreviewGeneration.Size, MapPreviewGeneration.Size,
                        "skipped", map.Error ?? "The map is not resolved.");
                    job.ProcessedCount++;
                    job.SkippedCount++;
                    continue;
                }

                if (MapPreviewGeneration.CanReuse(
                        previous,
                        old,
                        map,
                        File.Exists(oldImagePath),
                        force: requestedMapName is not null))
                {
                    File.Copy(oldImagePath, Path.Combine(stagingPath, $"{map.Name}.webp"));
                    entries[map.Name] = old!;
                    job.ProcessedCount++;
                }
                else
                {
                    changed.Add(new MapPreviewRenderMap(map.Name, map.Sha256));
                }
            }
            await context.SaveChangesAsync(cancellationToken);

            if (changed.Count > 0)
            {
                var renderResults = await RenderMapPreviewsAsync(
                    changed,
                    stagingPath,
                    async () =>
                    {
                        job.ProcessedCount++;
                        await context.SaveChangesAsync(cancellationToken);
                    },
                    cancellationToken);
                var results = renderResults.ToDictionary(
                    result => result.Name,
                    StringComparer.OrdinalIgnoreCase);
                foreach (var map in changed)
                {
                    results.TryGetValue(map.Name, out var result);
                    var imagePath = Path.Combine(stagingPath, $"{map.Name}.webp");
                    if (result?.Sha256 is not null && result.Error is null && File.Exists(imagePath))
                    {
                        entries[map.Name] = new MapPreviewCatalogEntry(
                            map.Name,
                            map.MapSourceHash,
                            $"/{EscapedUrlRoot(outputUrlRoot)}/{Uri.EscapeDataString(map.Name)}.webp",
                            MapPreviewGeneration.Size,
                            MapPreviewGeneration.Size,
                            "resolved",
                            null);
                    }
                    else
                    {
                        var error = result?.Error ?? "The renderer did not return a preview image.";
                        entries[map.Name] = new MapPreviewCatalogEntry(
                            map.Name, map.MapSourceHash, null, MapPreviewGeneration.Size, MapPreviewGeneration.Size,
                            "skipped", error);
                        warnings.Add($"{map.Name}: {error}");
                        job.SkippedCount++;
                    }
                }
            }

            job.WarningsJson = JsonSerializer.Serialize(warnings);
            await File.WriteAllTextAsync(Path.Combine(stagingPath, ".l2-asset-version"), job.SourceHash, cancellationToken);
            Promote(stagingPath, finalPath);
            var previewEntries = maps.Select(map => entries[map.Name]).ToArray();
            await PublishCatalogAsync(context, job, finalPath, AssetImportJobValues.MapPreviews, 1, null,
                Array.Empty<string>(), previewEntries, group => group, item => item.Name, _ => null,
                item => item.Status, new MapPreviewCatalogMetadata(MapPreviewGeneration.RendererVersion), cancellationToken);
            job.ProcessedCount = job.TotalCount;
            job.Status = warnings.Count == 0
                ? AssetImportJobValues.Succeeded
                : AssetImportJobValues.SucceededWithWarnings;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (Directory.Exists(stagingPath)) Directory.Delete(stagingPath, recursive: true);
        }
    }

    private static async Task<MapPreviewCatalogManifest?> ReadMapPreviewCatalogAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        var catalog = await context.AssetCatalogs.AsNoTracking().AsSplitQuery().Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion &&
                item.Kind == AssetImportJobValues.MapPreviews && item.IsActive, cancellationToken);
        if (catalog is null || catalog.SchemaVersion != 1) return null;
        var metadata = JsonSerializer.Deserialize<MapPreviewCatalogMetadata>(catalog.MetadataJson, ManifestJsonOptions);
        return new MapPreviewCatalogManifest(1, catalog.Kind, catalog.SourceHash,
            metadata?.RendererVersion ?? 0,
            catalog.Items.Select(item => JsonSerializer.Deserialize<MapPreviewCatalogEntry>(item.MetadataJson, ManifestJsonOptions)!).ToArray());
    }

    private async Task<IReadOnlyList<MapPreviewRenderResult>> RenderMapPreviewsAsync(
        IReadOnlyList<MapPreviewRenderMap> maps,
        string outputPath,
        Func<Task> onProgress,
        CancellationToken cancellationToken)
    {
        const int timeoutMilliseconds = 120_000;
        var browserWebSocketEndpoint = await ResolveBrowserWebSocketEndpointAsync(
            options.Value.MapPreviewBrowserUrl,
            cancellationToken);
        var browser = await Puppeteer.ConnectAsync(new ConnectOptions
        {
            BrowserWSEndpoint = browserWebSocketEndpoint,
            DefaultViewport = new ViewPortOptions
            {
                Width = MapPreviewGeneration.Size,
                Height = MapPreviewGeneration.Size,
                DeviceScaleFactor = 1
            },
            ProtocolTimeout = timeoutMilliseconds
        }).WaitAsync(cancellationToken);
        IBrowserContext? browserContext = null;
        try
        {
            browserContext = await browser.CreateBrowserContextAsync().WaitAsync(cancellationToken);
            var results = new List<MapPreviewRenderResult>(maps.Count);
            foreach (var map in maps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IPage? page = null;
                try
                {
                    page = await browserContext.NewPageAsync().WaitAsync(cancellationToken);
                    await page.SetViewportAsync(new ViewPortOptions
                    {
                        Width = MapPreviewGeneration.Size,
                        Height = MapPreviewGeneration.Size,
                        DeviceScaleFactor = 1
                    }).WaitAsync(cancellationToken);
                    var studioUrl = options.Value.StudioBaseUrl.TrimEnd('/');
                    var url = $"{studioUrl}/internal/map-preview/{Uri.EscapeDataString(map.Name)}";
                    await page.GoToAsync(url, new NavigationOptions
                    {
                        WaitUntil = [WaitUntilNavigation.DOMContentLoaded],
                        Timeout = timeoutMilliseconds
                    }).WaitAsync(cancellationToken);
                    await page.WaitForFunctionAsync(
                        "() => window.__l2MapPreview?.status === 'ready' || " +
                        "window.__l2MapPreview?.status === 'error'",
                        new WaitForFunctionOptions { Timeout = timeoutMilliseconds }).WaitAsync(cancellationToken);
                    var error = await page.EvaluateExpressionAsync<string?>(
                        "window.__l2MapPreview?.status === 'error' " +
                        "? window.__l2MapPreview.error : null").WaitAsync(cancellationToken);
                    if (error is not null) throw new InvalidOperationException(error);
                    await page.AddStyleTagAsync(new AddTagOptions
                    {
                        Content = """
                            #nuxt-devtools-container,
                            nuxt-devtools-inspect-panel,
                            vite-plugin-checker-error-overlay,
                            body > [role='region'] {
                              display: none !important;
                            }
                            """
                    }).WaitAsync(cancellationToken);
                    var canvas = await page.QuerySelectorAsync("canvas").WaitAsync(cancellationToken)
                        ?? throw new InvalidOperationException("The preview canvas is unavailable.");
                    var screenshot = await canvas.ScreenshotDataAsync(new ElementScreenshotOptions
                    {
                        Type = ScreenshotType.Png
                    }).WaitAsync(cancellationToken);
                    using var image = Image.Load(screenshot);
                    await using var encoded = new MemoryStream();
                    await image.SaveAsWebpAsync(encoded, new WebpEncoder
                    {
                        FileFormat = WebpFileFormatType.Lossy,
                        Quality = 85
                    }, cancellationToken);
                    var bytes = encoded.ToArray();
                    await File.WriteAllBytesAsync(
                        Path.Combine(outputPath, $"{map.Name}.webp"),
                        bytes,
                        cancellationToken);
                    results.Add(new MapPreviewRenderResult(
                        map.Name,
                        Convert.ToHexStringLower(SHA256.HashData(bytes)),
                        null));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    results.Add(new MapPreviewRenderResult(map.Name, null, exception.Message));
                }
                finally
                {
                    if (page is not null)
                    {
                        try
                        {
                            await page.CloseAsync().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                        }
                        catch (Exception exception)
                        {
                            logger.LogWarning(exception, "Failed to close preview page for {MapName}", map.Name);
                        }
                    }
                }
                await onProgress();
            }
            return results;
        }
        finally
        {
            if (browserContext is not null)
            {
                try
                {
                    await browserContext.CloseAsync().WaitAsync(TimeSpan.FromSeconds(5), CancellationToken.None);
                }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Failed to close the map-preview browser context");
                }
            }
            browser.Disconnect();
        }
    }

    private static async Task<string> ResolveBrowserWebSocketEndpointAsync(
        string browserUrl,
        CancellationToken cancellationToken)
    {
        var browserUri = new Uri(browserUrl.TrimEnd('/'), UriKind.Absolute);
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        await using var stream = await httpClient.GetStreamAsync(
            new Uri(browserUri, "/json/version"),
            cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var advertisedEndpoint = document.RootElement.GetProperty("webSocketDebuggerUrl").GetString()
            ?? throw new InvalidDataException("Chrome did not advertise a DevTools WebSocket endpoint.");
        var endpoint = new UriBuilder(advertisedEndpoint)
        {
            Scheme = browserUri.Scheme == Uri.UriSchemeHttps ? "wss" : "ws",
            Host = browserUri.Host,
            Port = browserUri.Port
        };
        return endpoint.Uri.AbsoluteUri;
    }

    private async Task ImportUnrealPackagesAsync(
        GameContentDbContext context,
        AssetImportJob job,
        bool scenes,
        CancellationToken cancellationToken)
    {
        var sourcePath = Path.GetFullPath(job.ConversionSourcePath ?? job.SourcePath);
        var assetRootPath = AssetRoot(job);
        if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
        {
            throw new DirectoryNotFoundException($"The configured map directory does not exist: {sourcePath}");
        }

        var mapPaths = SourceFiles(sourcePath, ".unr", scenes ? "scene" : "map")
            .Where(path => scenes
                ? UnrealPackageKindClassifier.IsScene(path)
                : UnrealPackageKindClassifier.IsWorldMap(path))
            .OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (mapPaths.Length == 0)
        {
            throw new InvalidOperationException(
                scenes
                    ? "The configured map directory contains no scene packages."
                    : "The configured map directory contains no coordinate-named map packages.");
        }

        var duplicateMap = mapPaths
            .GroupBy(Path.GetFileNameWithoutExtension, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateMap is not null)
        {
            throw new InvalidDataException(
                $"Map name '{duplicateMap.Key}' is duplicated ignoring case.");
        }

        var sources = new List<MapSource>(mapPaths.Length);
        foreach (var mapPath in mapPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fileName = Path.GetFileName(mapPath);
            var mapName = Path.GetFileNameWithoutExtension(fileName);
            RequireSafeSegment(mapName, "map name");
            var bytes = await File.ReadAllBytesAsync(mapPath, cancellationToken);
            sources.Add(new MapSource(
                mapPath,
                mapName,
                fileName,
                Convert.ToHexStringLower(SHA256.HashData(bytes))));
        }

        job.TotalCount = sources.Count;
        job.SourceHash = sources.Single().Sha256;
        await context.SaveChangesAsync(cancellationToken);

        var staticMeshes = await LoadStaticMeshLookupAsync(context, job.GameVersion, cancellationToken);
        var textures = await LoadTextureLookupAsync(context, job.GameVersion, cancellationToken);
        var sounds = await LoadSoundLookupAsync(context, job.GameVersion, cancellationToken);
        var sourceTexturePackages = new Dictionary<string, IReadOnlyDictionary<string, UnrealTexture>>(
            StringComparer.OrdinalIgnoreCase);
        var (finalPath, stagingPath, outputUrlRoot) = OutputPaths(assetRootPath, job);
        Directory.CreateDirectory(Path.GetDirectoryName(finalPath)!);
        Directory.CreateDirectory(stagingPath);
        var warnings = new List<string>();
        var catalogEntries = new List<MapCatalogEntry>();
        var sceneCatalogEntries = new List<SceneCatalogEntry>();
        IReadOnlyList<UnrealSkyZoneInfo> sharedSkyZones = [];
        if (scenes)
        {
            var skyPath = sources.FirstOrDefault(source =>
                string.Equals(source.Name, "skylevel", StringComparison.OrdinalIgnoreCase))?.Path;
            if (skyPath is null)
            {
                var sourceDirectory = Path.GetDirectoryName(Path.GetFullPath(job.SourcePath))!;
                skyPath = Directory.EnumerateFiles(sourceDirectory)
                    .FirstOrDefault(path => string.Equals(
                        Path.GetFileName(path), "skylevel.unr", StringComparison.OrdinalIgnoreCase));
            }
            if (skyPath is not null)
            {
                var skyBytes = await File.ReadAllBytesAsync(skyPath, cancellationToken);
                sharedSkyZones = new UnrealPackageReader(
                    LineagePackageDecoder.DecodeProtocol111(skyBytes)).ReadScene().SkyZones;
            }
        }
        try
        {
            foreach (var source in sources)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var decoded = LineagePackageDecoder.DecodeProtocol111(
                        await File.ReadAllBytesAsync(source.Path, cancellationToken));
                    var reader = new UnrealPackageReader(decoded);
                    var scene = scenes ? reader.ReadScene() : null;
                    var map = scene?.Level ?? reader.ReadLevel();
                    if (map.EnvironmentWarning is not null)
                    {
                        warnings.Add($"{source.FileName}: {map.EnvironmentWarning}");
                    }
                    var mapPath = Path.Combine(stagingPath, source.Name);
                    Directory.CreateDirectory(mapPath);
                    var effectiveSkyZones = scene is null || scene.SkyZones.Count > 0
                        ? scene?.SkyZones ?? []
                        : sharedSkyZones;
                    var skyBackdrops = scene is null
                        ? []
                        : await BuildSkyBackdropsAsync(
                            context,
                            scene.SkyBackdrops,
                            effectiveSkyZones,
                            mapPath,
                            outputUrlRoot,
                            source,
                            warnings,
                            cancellationToken);

                    var actors = await BuildActorManifestsAsync(
                        map.Actors,
                        staticMeshes,
                        mapPath,
                        outputUrlRoot,
                        source,
                        warnings,
                        cancellationToken);

                    var bspMeshes = await BuildBspManifestsAsync(
                        context,
                        job.GameVersion,
                        map.BspModels,
                        mapPath,
                        outputUrlRoot,
                        source,
                        warnings,
                        cancellationToken);

                    var terrains = new List<MapTerrainManifestEntry>();
                    foreach (var terrain in map.Terrains)
                    {
                        if (terrain.CoordinateFramesDerived)
                        {
                            warnings.Add(
                                $"{source.FileName}/{terrain.Name}: coordinate frames were derived from the authored terrain transform because the native frames were absent.");
                        }
                        string? terrainUrl = null;
                        string? heightmapPath = terrain.TerrainMap?.Path;
                        var heightmapWidth = 0;
                        var heightmapHeight = 0;
                        if (terrain.TerrainMap is not null)
                        {
                            var texture = await ReadSourceTextureAsync(
                                terrain.TerrainMap,
                                sourceTexturePackages,
                                job.GameVersion,
                                cancellationToken);
                            if (texture is not null)
                            {
                                heightmapWidth = texture.Width;
                                heightmapHeight = texture.Height;
                                var glb = GlbStaticMeshEncoder.Encode(
                                    UnrealTerrainMeshBuilder.Build(
                                        texture,
                                        terrain.ToWorld,
                                        terrain.Location));
                                var hash = Convert.ToHexStringLower(SHA256.HashData(glb));
                                var fileName = $"{terrain.Name}.glb";
                                await File.WriteAllBytesAsync(Path.Combine(mapPath, fileName), glb, cancellationToken);
                                terrainUrl = VersionedUrl(outputUrlRoot, source.Name, fileName, hash);
                            }
                            else
                            {
                                warnings.Add(
                                    $"{source.FileName}/{terrain.Name}: G16 heightmap '{terrain.TerrainMap.Path}' was not found.");
                            }
                        }
                        var material = await BuildTerrainMaterialAsync(
                            terrain,
                            mapPath,
                            outputUrlRoot,
                            source.Name,
                            textures,
                            sourceTexturePackages,
                            job.GameVersion,
                            cancellationToken);
                        if (material.Error is not null)
                        {
                            warnings.Add($"{source.FileName}/{terrain.Name}: {material.Error}");
                        }
                        if (material.Warning is not null)
                        {
                            warnings.Add($"{source.FileName}/{terrain.Name}: {material.Warning}");
                        }
                        terrains.Add(new MapTerrainManifestEntry(
                            terrain.Name,
                            Vec(terrain.Location),
                            Rot(default),
                            Vec(terrain.TerrainScale),
                            heightmapPath,
                            heightmapWidth,
                            heightmapHeight,
                            terrainUrl,
                            material.Layers,
                            material.ControlMapUrls,
                            material.ControlMapWidth,
                            material.ControlMapHeight,
                            "webp-rgb-a-horizontal",
                            0,
                            material.Error is null ? "resolved" : "skipped",
                            material.Error));
                    }

                    var waterVolumes = new List<MapWaterVolumeManifestEntry>();
                    foreach (var water in map.WaterVolumes)
                    {
                        string? meshUrl = null;
                        var error = water.Error;
                        if (water.Geometry is not null)
                        {
                            try
                            {
                                var mesh = new UnrealStaticMesh(
                                    water.Name,
                                    water.Geometry.Positions,
                                    water.Geometry.Normals,
                                    [],
                                    water.Geometry.Indices,
                                    []);
                                var glb = GlbStaticMeshEncoder.Encode(mesh);
                                var hash = Convert.ToHexStringLower(SHA256.HashData(glb));
                                var fileName = $"{water.Name}.glb";
                                await File.WriteAllBytesAsync(Path.Combine(mapPath, fileName), glb, cancellationToken);
                                meshUrl = VersionedUrl(outputUrlRoot, source.Name, fileName, hash);
                            }
                            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
                            {
                                error = exception.Message;
                            }
                        }
                        if (error is not null)
                            warnings.Add($"{source.FileName}/{water.Name}: {error}");
                        waterVolumes.Add(new MapWaterVolumeManifestEntry(
                            water.Name,
                            water.ClassName,
                            water.Brush?.ObjectName,
                            Vec(water.Location),
                            Rot(water.Rotation),
                            Vec(water.PrePivot),
                            water.DrawScale,
                            Vec(water.DrawScale3D),
                            meshUrl,
                            water.Geometry?.Positions.Count ?? 0,
                            water.Geometry?.TriangleCount ?? 0,
                            meshUrl is null ? "skipped" : "resolved",
                            error));
                    }

                    foreach (var unsupported in map.UnrepresentedObjectClasses.OrderBy(item => item.Key))
                    {
                        warnings.Add(
                            $"{source.FileName}: retained {unsupported.Value} unrepresented {unsupported.Key} objects as diagnostics.");
                    }

                    var lights = map.Lights.Select(light => new MapLightManifestEntry(
                            light.Name,
                            light.ClassName,
                            Vec(light.Location),
                            Rot(light.Rotation),
                            light.Brightness,
                            light.Hue,
                            light.Saturation,
                            light.Radius,
                            light.Properties)).ToArray();
                    var manifestFile = Path.Combine(mapPath, "manifest.json");
                    if (scene is null)
                    {
                        var manifestHash = await WriteManifestAsync(
                            manifestFile,
                            new MapManifest(
                                MapSchemaVersion,
                                source.Name,
                                source.FileName,
                                source.Sha256,
                                111,
                                PublishEnvironment(map),
                                terrains,
                                actors,
                                lights,
                                waterVolumes,
                                SceneSkyZones(map.SkyZones, textures, source, warnings),
                                bspMeshes,
                                map.UnrepresentedObjectClasses,
                                staticMeshes.GpuTextureFormats),
                            cancellationToken);
                        catalogEntries.Add(new MapCatalogEntry(
                            source.Name,
                            source.FileName,
                            $"/maps/{Uri.EscapeDataString(source.Name)}/manifest.json?v={manifestHash[..12]}",
                            terrains.Count,
                            actors.Length,
                            waterVolumes.Count,
                            source.Sha256,
                            "resolved",
                            null));
                    }
                    else
                    {
                        var manifestHash = await WriteManifestAsync(
                            manifestFile,
                            new SceneManifest(
                                SceneSchemaVersion,
                                source.Name,
                                source.FileName,
                                source.Sha256,
                                111,
                                PublishEnvironment(map),
                                terrains,
                                actors,
                                lights,
                                waterVolumes,
                                SceneSkyZones(effectiveSkyZones, textures, source, warnings),
                                bspMeshes,
                                skyBackdrops,
                                SceneObjects(scene.Cameras),
                                SceneObjects(scene.InterpolationPoints),
                                SceneObjects(scene.SceneManagers),
                                SceneObjects(scene.Actions),
                                SceneAmbientSounds(scene.AmbientSounds, sounds, source, warnings),
                                SceneEffects(scene.Effects, textures, source, warnings),
                                map.UnrepresentedObjectClasses,
                                staticMeshes.GpuTextureFormats),
                            cancellationToken);
                        sceneCatalogEntries.Add(new SceneCatalogEntry(
                            source.Name,
                            source.FileName,
                            $"/scenes/{Uri.EscapeDataString(source.Name)}/manifest.json?v={manifestHash[..12]}",
                            terrains.Count,
                            actors.Length,
                            SceneObjectCount(scene),
                            source.Sha256,
                            "resolved",
                            null));
                    }
                }
                catch (Exception exception) when (exception is InvalidDataException or OverflowException)
                {
                    warnings.Add($"{source.FileName}: {exception.Message}");
                    if (scenes)
                    {
                        sceneCatalogEntries.Add(new SceneCatalogEntry(
                            source.Name,
                            source.FileName,
                            null,
                            0,
                            0,
                            0,
                            source.Sha256,
                            "skipped",
                            exception.Message));
                    }
                    else
                    {
                        catalogEntries.Add(new MapCatalogEntry(
                            source.Name,
                            source.FileName,
                            null,
                            0,
                            0,
                            0,
                            source.Sha256,
                            "skipped",
                            exception.Message));
                    }
                    job.SkippedCount++;
                }

                job.ProcessedCount++;
                await context.SaveChangesAsync(cancellationToken);
            }

            job.WarningsJson = JsonSerializer.Serialize(warnings);
            await File.WriteAllTextAsync(Path.Combine(stagingPath, ".l2-asset-version"), job.SourceHash, cancellationToken);
            Promote(stagingPath, finalPath);
            if (scenes)
            {
                await PublishCatalogAsync(context, job, finalPath, "maps", SceneSchemaVersion, 111, Array.Empty<string>(), sceneCatalogEntries,
                    group => group, item => item.Name, _ => null, item => item.Status, new { }, cancellationToken);
            }
            else
            {
                await PublishCatalogAsync(context, job, finalPath, "maps", MapSchemaVersion, 111, Array.Empty<string>(), catalogEntries,
                    group => group, item => item.Name, _ => null, item => item.Status, new { }, cancellationToken);
            }
            job.Status = warnings.Count == 0
                ? AssetImportJobValues.Succeeded
                : AssetImportJobValues.SucceededWithWarnings;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, recursive: true);
            }
        }
    }

    private static SceneObjectManifestEntry[] SceneObjects(
        IReadOnlyList<UnrealSceneObject> objects) => objects
        .Select(item => new SceneObjectManifestEntry(
            item.Order,
            item.Name,
            item.ClassName,
            Vec(item.Location),
            Rot(item.Rotation),
            item.Duration,
            item.Target?.Path,
            item.Properties,
            item.Owner))
        .ToArray();

    private static async Task<SkyBackdropManifestEntry[]> BuildSkyBackdropsAsync(
        GameContentDbContext context,
        IReadOnlyList<UnrealSkyBackdrop> backdrops,
        IReadOnlyList<UnrealSkyZoneInfo> skyZones,
        string outputPath,
        string kind,
        MapSource source,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var activeSkyZone = skyZones.OrderBy(zone => zone.Order).LastOrDefault();
        var result = new List<SkyBackdropManifestEntry>(backdrops.Count);
        foreach (var backdrop in backdrops)
        {
            var error = backdrop.Error;
            string? meshUrl = null;
            if (backdrop.Mesh is not null)
            {
                try
                {
                    var materials = Enumerable.Repeat<StaticMeshMaterialBinding?>(
                        null,
                        backdrop.Mesh.Sections.Count).ToArray();
                    var glb = GlbStaticMeshEncoder.Encode(backdrop.Mesh, materials);
                    var hash = Convert.ToHexStringLower(SHA256.HashData(glb));
                    var fileName = $"{backdrop.Name}-sky-backdrop.glb";
                    await File.WriteAllBytesAsync(Path.Combine(outputPath, fileName), glb, cancellationToken);
                    meshUrl = VersionedUrl(kind, source.Name, fileName, hash);
                }
                catch (Exception exception) when (exception is InvalidDataException or OverflowException)
                {
                    error = exception.Message;
                }
            }
            if (error is not null) warnings.Add($"{source.FileName}/{backdrop.Name}: {error}");
            result.Add(new SkyBackdropManifestEntry(
                backdrop.Name,
                meshUrl,
                activeSkyZone?.Name,
                activeSkyZone?.TexUPanSpeed ?? 0,
                activeSkyZone?.TexVPanSpeed ?? 0,
                false,
                error));
        }
        return result.ToArray();
    }

    private static async Task<MapBspMeshManifestEntry[]> BuildBspManifestsAsync(
        GameContentDbContext context,
        string gameVersion,
        IReadOnlyList<UnrealBspModel> models,
        string outputPath,
        string kind,
        MapSource source,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var references = models
            .SelectMany(model => model.Chunks)
            .SelectMany(chunk => chunk.Mesh.Sections)
            .Select(section => MaterialReference(source.Name, section.Material))
            .OfType<TextureMaterialReference>()
            .ToArray();
        var catalog = await StaticMeshMaterialCatalogLoader.LoadAsync(
            context,
            gameVersion,
            references,
            cancellationToken);
        var result = new List<MapBspMeshManifestEntry>();
        foreach (var model in models)
        {
            if (model.Error is not null)
            {
                warnings.Add($"{source.FileName}/{model.Name}: {model.Error}");
                result.Add(BspEntry(model, null, null, 0, 0, "unresolved", model.Error, true));
                continue;
            }

            var skipped = model.Diagnostics.InvisibleSurfaceCount +
                model.Diagnostics.PortalSurfaceCount +
                model.Diagnostics.FakeBackdropSurfaceCount;
            if (skipped > 0 || model.Diagnostics.MalformedSurfaceCount > 0)
            {
                warnings.Add(
                    $"{source.FileName}/{model.Name}: BSP skipped {skipped} render-excluded and " +
                    $"{model.Diagnostics.MalformedSurfaceCount} malformed surfaces.");
            }
            if (model.Diagnostics.UnresolvedMaterialReferenceCount > 0)
            {
                warnings.Add(
                    $"{source.FileName}/{model.Name}: BSP retained {model.Diagnostics.UnresolvedMaterialReferenceCount} surfaces with invalid material references using neutral fallback.");
            }

            var firstChunk = true;
            foreach (var chunk in model.Chunks)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string? meshUrl = null;
                string? error = null;
                var resolved = catalog.Resolver.Resolve(chunk.Mesh, source.Name);
                if (resolved.Error is not null) error = resolved.Error;
                try
                {
                    var materials = resolved.SectionMaterials
                        .Select(material => ApplyBspFlags(material, chunk.RenderFlags))
                        .ToArray();
                    var glb = GlbStaticMeshEncoder.Encode(chunk.Mesh, materials);
                    var hash = Convert.ToHexStringLower(SHA256.HashData(glb));
                    var fileName = $"{chunk.Name}.glb";
                    await File.WriteAllBytesAsync(
                        Path.Combine(outputPath, fileName),
                        glb,
                        cancellationToken);
                    meshUrl = VersionedUrl(kind, source.Name, fileName, hash);
                }
                catch (Exception exception) when (exception is InvalidDataException or OverflowException)
                {
                    error = exception.Message;
                }
                if (error is not null)
                    warnings.Add($"{source.FileName}/{chunk.Name}: {error}");
                result.Add(BspEntry(
                    model,
                    chunk,
                    meshUrl,
                    resolved.MaterialCount,
                    resolved.ResolvedMaterialCount,
                    resolved.Status,
                    error,
                    firstChunk));
                firstChunk = false;
            }
        }
        return result.ToArray();
    }

    internal static StaticMeshMaterialBinding? ApplyBspFlags(
        StaticMeshMaterialBinding? material,
        UnrealPolyFlags flags)
    {
        var materialFlags = flags & (
            UnrealPolyFlags.Masked |
            UnrealPolyFlags.Translucent |
            UnrealPolyFlags.Modulated |
            UnrealPolyFlags.TwoSided |
            UnrealPolyFlags.Unlit);
        if (material is null && materialFlags == UnrealPolyFlags.None) return null;
        material ??= new StaticMeshMaterialBinding(
            "BSP neutral fallback",
            null,
            null,
            null,
            StaticMeshBlendMode.Opaque,
            false,
            0.5f,
            true,
            true);
        var blendMode = (flags & UnrealPolyFlags.Masked) != 0
            ? StaticMeshBlendMode.Masked
            : (flags & UnrealPolyFlags.Translucent) != 0
                ? StaticMeshBlendMode.AlphaBlend
                : (flags & UnrealPolyFlags.Modulated) != 0
                    ? StaticMeshBlendMode.Modulate
                    : material.BlendMode;
        return material with
        {
            BlendMode = blendMode,
            DoubleSided = material.DoubleSided ||
                (flags & UnrealPolyFlags.TwoSided) != 0,
            Unlit = material.Unlit || (flags & UnrealPolyFlags.Unlit) != 0
        };
    }

    private static MapBspMeshManifestEntry BspEntry(
        UnrealBspModel model,
        UnrealBspMeshChunk? chunk,
        string? meshUrl,
        int materialCount,
        int resolvedMaterialCount,
        string materialStatus,
        string? error,
        bool includeDiagnostics) => new(
            chunk?.Name ?? model.Name,
            model.Name,
            chunk?.Role switch
            {
                UnrealBspMeshRole.SkyZone => "sky-zone",
                UnrealBspMeshRole.WaterSurface => "water-surface",
                UnrealBspMeshRole.WorldBase => "world-base",
                _ => "geometry"
            },
            chunk?.SkyZoneName,
            chunk?.WaterVolumeNames ?? [],
            meshUrl,
            chunk?.Mesh.Positions.Count ?? 0,
            chunk?.Mesh.Indices.Count / 3 ?? 0,
            chunk?.SurfaceCount ?? 0,
            materialCount,
            resolvedMaterialCount,
            materialStatus,
            (uint)(chunk?.RenderFlags ?? UnrealPolyFlags.None),
            includeDiagnostics ? model.Diagnostics.SplitterNodeCount : 0,
            includeDiagnostics ? model.Diagnostics.InvisibleSurfaceCount : 0,
            includeDiagnostics ? model.Diagnostics.PortalSurfaceCount : 0,
            includeDiagnostics ? model.Diagnostics.FakeBackdropSurfaceCount : 0,
            includeDiagnostics ? model.Diagnostics.MalformedSurfaceCount : 0,
            includeDiagnostics ? model.Diagnostics.UnresolvedMaterialReferenceCount : 0,
            error);

    private static SkyZoneManifestEntry[] SceneSkyZones(
        IReadOnlyList<UnrealSkyZoneInfo> skyZones,
        IReadOnlyDictionary<string, PublishedTexture> textures,
        MapSource source,
        List<string> warnings) => skyZones.Select(skyZone =>
    {
        var lensFlares = skyZone.LensFlares.Select(flare =>
        {
            textures.TryGetValue(
                TextureKey(flare.Texture.PackageName, flare.Texture.ObjectName),
                out var texture);
            if (texture?.Url is null)
            {
                warnings.Add(
                    $"{source.FileName}/{skyZone.Name}: lens flare texture '{flare.Texture.Path}' is not published.");
            }
            return new SkyZoneLensFlareManifestEntry(
                flare.Index,
                flare.Texture.PackageName,
                flare.Texture.ObjectName,
                texture?.Url,
                flare.Offset,
                flare.Scale);
        }).ToArray();
        return new SkyZoneManifestEntry(
            skyZone.Order,
            skyZone.Name,
            Vec(skyZone.Location),
            skyZone.DrawScale,
            skyZone.TexUPanSpeed,
            skyZone.TexVPanSpeed,
            lensFlares);
    }).ToArray();

    private static SceneObjectManifestEntry[] SceneEffects(
        IReadOnlyList<UnrealSceneObject> objects,
        IReadOnlyDictionary<string, PublishedTexture> textures,
        MapSource source,
        List<string> warnings)
    {
        var owners = objects
            .Where(item => item.ClassName == "Emitter")
            .ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
        return objects.Select(item =>
        {
            var location = item.Location;
            var rotation = item.Rotation;
            if (item.Owner is not null && owners.TryGetValue(item.Owner, out var owner))
            {
                location = owner.Location;
                rotation = owner.Rotation;
            }
            var properties = item.Properties;
            if (item.Owner is not null && owners.TryGetValue(item.Owner, out var owningEmitter))
            {
                properties = new Dictionary<string, string>(item.Properties, StringComparer.OrdinalIgnoreCase)
                {
                    ["OwnerDrawScale"] = owningEmitter.Properties.GetValueOrDefault("DrawScale", "1")
                };
            }
            string? resourceUrl = null;
            var texturePath = new[] { "Texture", "ProjTexture", "ProjectorMaterial", "Skins[0]", "Skin" }
                .Select(key => item.Properties.GetValueOrDefault(key))
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
            if (texturePath is not null)
            {
                var separator = texturePath.IndexOf('.');
                if (separator > 0 && separator < texturePath.Length - 1)
                {
                    textures.TryGetValue(
                        TextureKey(texturePath[..separator], texturePath[(separator + 1)..]),
                        out var texture);
                    resourceUrl = texture?.Url;
                }
                if (resourceUrl is null && !item.Properties.ContainsKey("Disabled"))
                {
                    warnings.Add(
                        $"{source.FileName}/{item.Name}: effect texture '{texturePath}' is not published.");
                }
            }
            return new SceneObjectManifestEntry(
                item.Order,
                item.Name,
                item.ClassName,
                Vec(location),
                Rot(rotation),
                item.Duration,
                item.Target?.Path,
                properties,
                item.Owner,
                resourceUrl,
                ParticleSettings(item, properties));
        }).ToArray();
    }

    internal static ParticleEmitterManifestEntry? ParticleSettings(
        UnrealSceneObject item,
        IReadOnlyDictionary<string, string> properties)
    {
        if (item.ClassName is not ("SpriteEmitter" or "BeamEmitter")) return null;
        var diagnostics = new List<string>();
        var lifetime = Range(properties, "LifetimeRange", new ParticleNumberRange(4, 4), diagnostics);
        var size = VectorRange(properties, "StartSizeRange", new ParticleVectorRange(
            new MapVector(100, 100, 100), new MapVector(100, 100, 100)), diagnostics);
        var velocity = VectorRange(properties, "StartVelocityRange", new ParticleVectorRange(
            new MapVector(0, 0, 0), new MapVector(0, 0, 0)), diagnostics);
        var location = VectorRange(properties, "StartLocationRange", new ParticleVectorRange(
            new MapVector(0, 0, 0), new MapVector(0, 0, 0)), diagnostics);
        var spinVector = VectorRange(properties, "SpinsPerSecondRange", new ParticleVectorRange(
            new MapVector(0, 0, 0), new MapVector(0, 0, 0)), diagnostics);
        var spin = new ParticleNumberRange(spinVector.Min.X, spinVector.Max.X);
        var capacity = Math.Clamp(Int(properties, "MaxParticles", 100, diagnostics), 1, 2000);
        var emitRate = Number(properties, "ParticlesPerSecond", capacity / Math.Max(lifetime.Max, 0.05f), diagnostics);
        var subdivisionsU = Math.Max(Int(properties, "TextureUSubdivisions", 1, diagnostics), 1);
        var subdivisionsV = Math.Max(Int(properties, "TextureVSubdivisions", 1, diagnostics), 1);
        var isBeam = item.ClassName == "BeamEmitter";
        return new ParticleEmitterManifestEntry(
            isBeam ? "beam" : "sprite",
            !Bool(properties, "Disabled", false),
            capacity,
            ParticleDrawStyle(properties.GetValueOrDefault("DrawStyle", "3"), diagnostics),
            Number(properties, "Opacity", 1, diagnostics),
            lifetime,
            size,
            velocity,
            location,
            Vector(properties, "StartLocationOffset", new MapVector(0, 0, 0), diagnostics),
            Vector(properties, "Acceleration", new MapVector(0, 0, 0), diagnostics),
            Math.Max(emitRate, 0.1f),
            Bool(properties, "SpinParticles", false),
            spin,
            Vector(properties, "SpinCCWorCW", new MapVector(0.5f, 0.5f, 0.5f), diagnostics),
            new ParticleTextureSubdivisions(
                subdivisionsU,
                subdivisionsV,
                Bool(properties, "UseRandomSubdivision", false)),
            SizeCurve(properties, diagnostics),
            ColorCurve(properties, diagnostics),
            Math.Max(properties.ContainsKey("WarmupTime")
                ? Number(properties, "WarmupTime", 0, diagnostics)
                : Number(properties, "RelativeWarmupTime", 0, diagnostics), 0),
            Math.Max(Number(properties, "WarmupTicksPerSecond", 0, diagnostics), 0),
            isBeam ? null : new ParticleSpriteSettings(
                ParticleDirectionMode(properties.GetValueOrDefault("UseDirectionAs", "0"), diagnostics),
                ParticleLocationShape(properties.GetValueOrDefault("StartLocationShape", "0"), diagnostics),
                Range(properties, "SphereRadiusRange", new ParticleNumberRange(0, 0), diagnostics),
                ParticleRotationSource(properties.GetValueOrDefault("UseRotationFrom", "0"), diagnostics),
                Math.Max(Int(properties, "ColorScaleRepeats", 1, diagnostics), 1)),
            isBeam ? new ParticleBeamSettings(
                ParticleBeamMode(properties.GetValueOrDefault("DetermineEndPointBy", "none"), diagnostics),
                BeamEndPoints(properties, diagnostics),
                Number(properties, "BeamTextureUScale", 1, diagnostics),
                Number(properties, "BeamTextureVScale", 1, diagnostics),
                Math.Max(Int(properties, "RotatingSheets", 1, diagnostics), 1)) : null,
            diagnostics);
    }

    private static string ParticleDrawStyle(string value, List<string> diagnostics) =>
        NormalizeParticleEnum(value, "DrawStyle", diagnostics, "translucent", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["1"] = "alpha-blend",
            ["PTDS_AlphaBlend"] = "alpha-blend",
            ["3"] = "translucent",
            ["PTDS_Translucent"] = "translucent",
            ["5"] = "darken",
            ["PTDS_Darken"] = "darken",
            ["6"] = "brighten",
            ["PTDS_Brighten"] = "brighten"
        });

    private static string ParticleDirectionMode(string value, List<string> diagnostics) =>
        NormalizeParticleEnum(value, "UseDirectionAs", diagnostics, "unsupported", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["0"] = "none",
            ["none"] = "none",
            ["PTDU_None"] = "none",
            ["1"] = "up",
            ["PTDU_Up"] = "up",
            ["4"] = "normal",
            ["PTDU_Normal"] = "normal"
        });

    private static string ParticleLocationShape(string value, List<string> diagnostics) =>
        NormalizeParticleEnum(value, "StartLocationShape", diagnostics, "unsupported", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["0"] = "box",
            ["PTLS_Box"] = "box",
            ["1"] = "sphere",
            ["PTLS_Sphere"] = "sphere"
        });

    private static string ParticleRotationSource(string value, List<string> diagnostics) =>
        NormalizeParticleEnum(value, "UseRotationFrom", diagnostics, "unsupported", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["0"] = "none",
            ["none"] = "none",
            ["PTRS_None"] = "none",
            ["3"] = "normal",
            ["PTRS_Normal"] = "normal"
        });

    private static string ParticleBeamMode(string value, List<string> diagnostics) =>
        NormalizeParticleEnum(value, "DetermineEndPointBy", diagnostics, "unsupported", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["2"] = "offset",
            ["PTEP_Offset"] = "offset"
        });

    private static string NormalizeParticleEnum(
        string value,
        string name,
        List<string> diagnostics,
        string fallback,
        IReadOnlyDictionary<string, string> values)
    {
        if (values.TryGetValue(value, out var normalized)) return normalized;
        diagnostics.Add($"{name} value '{value}' is unsupported; {fallback} behavior will be used.");
        return fallback;
    }

    private static ParticleNumberRange Range(
        IReadOnlyDictionary<string, string> properties,
        string name,
        ParticleNumberRange fallback,
        List<string> diagnostics)
    {
        if (!properties.TryGetValue(name, out var text)) return fallback;
        var values = text.Split(',');
        if (values.Length == 2 && TryNumber(values[0], out var min) && TryNumber(values[1], out var max))
            return new ParticleNumberRange(Math.Min(min, max), Math.Max(min, max));
        diagnostics.Add($"{name} is malformed and the UE2 default was used.");
        return fallback;
    }

    private static ParticleVectorRange VectorRange(
        IReadOnlyDictionary<string, string> properties,
        string name,
        ParticleVectorRange fallback,
        List<string> diagnostics)
    {
        if (!properties.TryGetValue(name, out var text)) return fallback;
        var values = text.Split(';');
        if (values.Length == 2 && TryVector(values[0], out var min) && TryVector(values[1], out var max))
            return new ParticleVectorRange(min, max);
        diagnostics.Add($"{name} is malformed and the UE2 default was used.");
        return fallback;
    }

    private static MapVector Vector(
        IReadOnlyDictionary<string, string> properties,
        string name,
        MapVector fallback,
        List<string> diagnostics)
    {
        if (!properties.TryGetValue(name, out var text)) return fallback;
        if (TryVector(text, out var value)) return value;
        diagnostics.Add($"{name} is malformed and the UE2 default was used.");
        return fallback;
    }

    private static bool TryVector(string text, out MapVector value)
    {
        var values = text.Split(',');
        if (values.Length == 3 && TryNumber(values[0], out var x) && TryNumber(values[1], out var y) && TryNumber(values[2], out var z))
        {
            value = new MapVector(x, y, z);
            return true;
        }
        value = new MapVector(0, 0, 0);
        return false;
    }

    private static float Number(
        IReadOnlyDictionary<string, string> properties,
        string name,
        float fallback,
        List<string> diagnostics)
    {
        if (!properties.TryGetValue(name, out var text)) return fallback;
        if (TryNumber(text, out var value)) return value;
        diagnostics.Add($"{name} is malformed and the UE2 default was used.");
        return fallback;
    }

    private static int Int(
        IReadOnlyDictionary<string, string> properties,
        string name,
        int fallback,
        List<string> diagnostics)
    {
        if (!properties.TryGetValue(name, out var text)) return fallback;
        if (int.TryParse(text, System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture, out var value)) return value;
        diagnostics.Add($"{name} is malformed and the UE2 default was used.");
        return fallback;
    }

    private static bool Bool(IReadOnlyDictionary<string, string> properties, string name, bool fallback) =>
        properties.TryGetValue(name, out var text) && bool.TryParse(text, out var value) ? value : fallback;

    private static bool TryNumber(string text, out float value) => float.TryParse(
        text,
        System.Globalization.NumberStyles.Float,
        System.Globalization.CultureInfo.InvariantCulture,
        out value) && float.IsFinite(value);

    private static ParticleBeamEndPoint[] BeamEndPoints(
        IReadOnlyDictionary<string, string> properties,
        List<string> diagnostics)
    {
        if (!properties.TryGetValue("BeamEndPoints", out var text) || string.IsNullOrWhiteSpace(text))
        {
            diagnostics.Add("BeamEndPoints is missing or contains no supported offset endpoints.");
            return [];
        }
        var result = new List<ParticleBeamEndPoint>();
        foreach (var endpoint in text.Split('|'))
        {
            var values = endpoint.Split(';');
            if (values.Length == 3 && TryVector(values[0], out var min) &&
                TryVector(values[1], out var max) && TryNumber(values[2], out var weight))
            {
                result.Add(new ParticleBeamEndPoint(
                    new ParticleVectorRange(min, max),
                    Math.Max(weight, 0)));
            }
            else diagnostics.Add("BeamEndPoints contains a malformed endpoint that was ignored.");
        }
        return result.ToArray();
    }

    private static ParticleSizeCurveKey[] SizeCurve(
        IReadOnlyDictionary<string, string> properties,
        List<string> diagnostics)
    {
        if (!Bool(properties, "UseSizeScale", false) &&
            !Bool(properties, "UseRegularSizeScale", false)) return [];
        if (!properties.TryGetValue("SizeScale", out var text) || string.IsNullOrWhiteSpace(text))
        {
            diagnostics.Add("SizeScale is enabled but has no supported curve keys.");
            return [];
        }
        var result = new List<ParticleSizeCurveKey>();
        foreach (var key in text.Split(';'))
        {
            var values = key.Split(',');
            if (values.Length == 2 && TryNumber(values[0], out var time) &&
                TryNumber(values[1], out var relativeSize))
                result.Add(new ParticleSizeCurveKey(Math.Clamp(time, 0, 1), relativeSize));
            else diagnostics.Add("SizeScale contains a malformed key that was ignored.");
        }
        return result.OrderBy(key => key.Time).ToArray();
    }

    private static ParticleColorCurveKey[] ColorCurve(
        IReadOnlyDictionary<string, string> properties,
        List<string> diagnostics)
    {
        if (!Bool(properties, "UseColorScale", false)) return [];
        if (!properties.TryGetValue("ColorScale", out var text) || string.IsNullOrWhiteSpace(text))
        {
            diagnostics.Add("ColorScale is enabled but has no supported curve keys.");
            return [];
        }
        var result = new List<ParticleColorCurveKey>();
        foreach (var key in text.Split(';'))
        {
            var values = key.Split(',');
            if (values.Length == 5 && TryNumber(values[0], out var time) &&
                byte.TryParse(values[1], out var red) && byte.TryParse(values[2], out var green) &&
                byte.TryParse(values[3], out var blue) && byte.TryParse(values[4], out var alpha))
                result.Add(new ParticleColorCurveKey(
                    Math.Clamp(time, 0, 1),
                    new MapColorWithAlpha(red / 255f, green / 255f, blue / 255f, alpha / 255f)));
            else diagnostics.Add("ColorScale contains a malformed key that was ignored.");
        }
        return result.OrderBy(key => key.Time).ToArray();
    }

    private static SceneObjectManifestEntry[] SceneAmbientSounds(
        IReadOnlyList<UnrealSceneObject> objects,
        IReadOnlyDictionary<string, string> sounds,
        MapSource source,
        List<string> warnings) => objects.Select(item =>
    {
        string? resourceUrl = null;
        string? diagnostic = null;
        if (item.Properties.TryGetValue("AmbientSound", out var soundPath))
        {
            var separator = soundPath.IndexOf('.');
            if (separator > 0 && separator < soundPath.Length - 1)
                sounds.TryGetValue(TextureKey(soundPath[..separator], soundPath[(separator + 1)..]), out resourceUrl);
            if (resourceUrl is null)
            {
                diagnostic = $"ambient sound '{soundPath}' is not published.";
                warnings.Add($"{source.FileName}/{item.Name}: {diagnostic}");
            }
        }
        else
        {
            diagnostic = "no AmbientSound reference is authored; the source Group value is not a playable sound object.";
            warnings.Add($"{source.FileName}/{item.Name}: {diagnostic}");
        }
        return new SceneObjectManifestEntry(
            item.Order,
            item.Name,
            item.ClassName,
            Vec(item.Location),
            Rot(item.Rotation),
            item.Duration,
            item.Target?.Path,
            item.Properties,
            item.Owner,
            resourceUrl,
            null,
            diagnostic);
    }).ToArray();

    private static int SceneObjectCount(UnrealScene scene) =>
        scene.Cameras.Count +
        scene.InterpolationPoints.Count +
        scene.SceneManagers.Count +
        scene.Actions.Count +
        scene.AmbientSounds.Count +
        scene.Effects.Count;

    private async Task<TerrainMaterialBuild> BuildTerrainMaterialAsync(
        UnrealTerrainInfo terrain,
        string outputPath,
        string kind,
        string mapName,
        IReadOnlyDictionary<string, PublishedTexture> textureUrls,
        Dictionary<string, IReadOnlyDictionary<string, UnrealTexture>> sourceTexturePackages,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        var selection = TerrainLayerSelector.SelectCompletePrefix(terrain.Layers);
        if (selection.Error is not null)
        {
            return new TerrainMaterialBuild([], [], 0, 0, selection.Error);
        }
        var layers = selection.Layers.ToArray();
        var diagnostics = new List<string>();
        if (selection.IgnoredTrailingLayerIndices.Count > 0)
        {
            diagnostics.Add(
                $"Ignored trailing incomplete terrain layer slots {string.Join(", ", selection.IgnoredTrailingLayerIndices)}.");
        }
        string? Warning() => diagnostics.Count == 0 ? null : string.Join(" ", diagnostics);
        TerrainMaterialBuild Failure(string error) => new([], [], 0, 0, error, Warning());
        if (layers.Any(layer => layer.TextureMapAxis > 2))
        {
            return Failure("Terrain uses an unknown texture-map axis.");
        }

        var alphaMaps = new List<UnrealTexture>(layers.Length);
        var entries = new List<MapTerrainLayerManifestEntry>(layers.Length);
        var textureGroups = new Dictionary<(int Width, int Height), int>();
        var textureGroupLayers = new Dictionary<int, int>();
        PublishedTexture? neutralTexture = null;
        async Task<PublishedTexture> NeutralTextureAsync()
        {
            if (neutralTexture is not null) return neutralTexture;
            const int size = 4;
            var pixels = Enumerable.Repeat(new Rgba32(128, 128, 128, byte.MaxValue), size * size).ToArray();
            var bytes = await WebpTextureEncoder.EncodeRgbaDataLosslessAsync(
                pixels,
                size,
                size,
                cancellationToken);
            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var fileName = $"{terrain.Name}.neutral.webp";
            await File.WriteAllBytesAsync(Path.Combine(outputPath, fileName), bytes, cancellationToken);
            neutralTexture = new PublishedTexture(
                VersionedUrl(kind, mapName, fileName, hash, gpuTextureAvailable: false),
                size,
                size);
            return neutralTexture;
        }
        for (var index = 0; index < layers.Length; index++)
        {
            var layer = layers[index];
            var texture = layer.Texture!;
            var alpha = layer.AlphaMap!;
            UnrealTerrainUvTransform uvTransform;
            try
            {
                uvTransform = UnrealTerrainUvTransformBuilder.Build(
                    terrain.ToWorld,
                    terrain.ToHeightMap,
                    terrain.Location,
                    layer);
            }
            catch (Exception exception) when (exception is InvalidDataException or OverflowException)
            {
                return Failure(exception.Message);
            }
            textureUrls.TryGetValue(TextureKey(texture.PackageName, texture.ObjectName), out var publishedTexture);
            var alphaMap = await ReadSourceTextureAsync(
                alpha,
                sourceTexturePackages,
                gameVersion,
                cancellationToken);
            if (alphaMap is null)
            {
                return Failure($"Terrain material dependency '{alpha.Path}' is not published.");
            }
            if (publishedTexture is null)
            {
                publishedTexture = await NeutralTextureAsync();
                diagnostics.Add(
                    $"Terrain texture '{texture.Path}' is not published; using the neutral fallback.");
            }
            var dimensions = (publishedTexture.Width, publishedTexture.Height);
            if (!textureGroups.TryGetValue(dimensions, out var textureArrayGroup))
            {
                textureArrayGroup = textureGroups.Count;
                textureGroups[dimensions] = textureArrayGroup;
            }
            var textureArrayLayer = textureGroupLayers.GetValueOrDefault(textureArrayGroup);
            textureGroupLayers[textureArrayGroup] = textureArrayLayer + 1;
            alphaMaps.Add(alphaMap);
            entries.Add(new MapTerrainLayerManifestEntry(
                layer.Index,
                texture.PackageName,
                texture.ObjectName,
                publishedTexture.Url,
                publishedTexture.Width,
                publishedTexture.Height,
                textureArrayGroup,
                textureArrayLayer,
                alpha.PackageName,
                alpha.ObjectName,
                index / TerrainControlMapEncoder.ChannelsPerMap,
                index % TerrainControlMapEncoder.ChannelsPerMap,
                layer.UScale,
                layer.VScale,
                layer.UPan,
                layer.VPan,
                TextureMapAxis(layer.TextureMapAxis),
                layer.TextureRotation,
                Rot(layer.LayerRotation),
                new MapTerrainUvTransform(
                    new MapTerrainUvTransformRow(
                        uvTransform.U.X,
                        uvTransform.U.Y,
                        uvTransform.U.Z,
                        uvTransform.U.Offset),
                    new MapTerrainUvTransformRow(
                        uvTransform.V.X,
                        uvTransform.V.Y,
                        uvTransform.V.Z,
                        uvTransform.V.Offset))));
        }

        var controlMapUrls = new List<string>();
        var controlMaps = TerrainControlMapEncoder.Pack(alphaMaps);
        for (var index = 0; index < controlMaps.Count; index++)
        {
            var controlMap = controlMaps[index];
            var encoded = TerrainControlMapEncoder.EncodeOpaqueTransport(controlMap);
            var bytes = await WebpTextureEncoder.EncodeRgbaDataLosslessAsync(
                encoded.Pixels.ToArray(),
                encoded.Width,
                encoded.Height,
                cancellationToken);
            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var fileName = $"{terrain.Name}.control-{index}.webp";
            await File.WriteAllBytesAsync(Path.Combine(outputPath, fileName), bytes, cancellationToken);
            controlMapUrls.Add(VersionedUrl(kind, mapName, fileName, hash, gpuTextureAvailable: false));
        }

        return new TerrainMaterialBuild(
            entries,
            controlMapUrls,
            controlMaps[0].Width,
            controlMaps[0].Height,
            null,
            Warning());
    }

    private async Task<UnrealTexture?> ReadSourceTextureAsync(
        UnrealObjectReference reference,
        Dictionary<string, IReadOnlyDictionary<string, UnrealTexture>> packages,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        if (packages.TryGetValue(reference.PackageName, out var cached))
        {
            return cached.GetValueOrDefault(reference.ObjectName);
        }
        var textureDirectory = Path.Combine(
            Path.GetFullPath(options.Value.SourceRootPath),
            gameVersion == "c1" ? "C1" : gameVersion == "c4" ? "C4" : "Interlude",
            "textures");
        var path = Directory.EnumerateFiles(textureDirectory, "*.utx")
            .SingleOrDefault(candidate => string.Equals(
                Path.GetFileNameWithoutExtension(candidate),
                reference.PackageName,
                StringComparison.OrdinalIgnoreCase));
        if (path is null)
        {
            packages[reference.PackageName] = new Dictionary<string, UnrealTexture>(StringComparer.OrdinalIgnoreCase);
            return null;
        }
        var fileName = Path.GetFileName(path);
        var decoded = LineagePackageDecoder.DecodeProtocol121(
            await File.ReadAllBytesAsync(path, cancellationToken),
            fileName);
        var textures = new UnrealPackageReader(decoded).ReadTextures().ToDictionary(
            texture => texture.Name,
            StringComparer.OrdinalIgnoreCase);
        packages[reference.PackageName] = textures;
        return textures.GetValueOrDefault(reference.ObjectName);
    }

    private static async Task<Dictionary<string, PublishedTexture>> LoadTextureLookupAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        var catalog = await context.AssetCatalogs.AsNoTracking()
            .AsSplitQuery()
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion &&
                item.Kind == AssetImportJobValues.Textures && item.IsActive, cancellationToken);
        var textures = catalog?.Items.Select(item => JsonSerializer.Deserialize<TextureManifestEntry>(item.MetadataJson, ManifestJsonOptions)!).ToArray() ?? [];
        var metadata = catalog is null ? null : JsonSerializer.Deserialize<TextureCatalogMetadata>(catalog.MetadataJson, ManifestJsonOptions);
        var lookup = textures
            .Where(texture => texture.Url is not null)
            .ToDictionary(
                texture => TextureKey(texture.PackageName, texture.ObjectName),
                texture => new PublishedTexture(texture.Url!, texture.Width, texture.Height),
            StringComparer.OrdinalIgnoreCase);
        var materials = metadata?.Materials.ToDictionary(
            material => TextureKey(material.PackageName, material.ObjectName),
            StringComparer.OrdinalIgnoreCase) ?? [];
        PublishedTexture? ResolveMaterial(
            TextureMaterialManifestEntry material,
            HashSet<string> visited)
        {
            var key = TextureKey(material.PackageName, material.ObjectName);
            if (!visited.Add(key) || visited.Count > 16) return null;
            var reference = material.ClassName switch
            {
                "FinalBlend" or "Panner" or "Rotator" or "TexPanner" or "TexRotator" or "Combiner" or "TexOscillator" or "TexOscillatorTriggered" or "ColorModifier" => material.Material,
                "FadeColor" => null,
                _ => material.Diffuse
            };
            if (reference is null) return null;
            var packageName = string.IsNullOrEmpty(reference.PackageName)
                ? material.PackageName
                : reference.PackageName;
            var referenceKey = TextureKey(packageName, reference.ObjectName);
            return lookup.GetValueOrDefault(referenceKey) ??
                (materials.TryGetValue(referenceKey, out var inner)
                    ? ResolveMaterial(inner, visited)
                    : null);
        }
        foreach (var material in materials.Values)
        {
            var resolved = ResolveMaterial(material, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
            if (resolved is not null)
                lookup[TextureKey(material.PackageName, material.ObjectName)] = resolved;
        }
        return lookup;
    }

    private static async Task<Dictionary<string, string>> LoadSoundLookupAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        var items = await ActiveCatalogItemJsonAsync(
            context, gameVersion, AssetImportJobValues.Sounds, cancellationToken);
        return items.Select(item => JsonSerializer.Deserialize<SoundManifestEntry>(item, ManifestJsonOptions)!)
            .ToDictionary(
            sound => TextureKey(sound.PackageName, sound.ObjectName),
            sound => sound.Url,
            StringComparer.OrdinalIgnoreCase);
    }

    private static async Task<StaticMeshLookup> LoadStaticMeshLookupAsync(
        GameContentDbContext context,
        string gameVersion,
        CancellationToken cancellationToken)
    {
        var catalog = await context.AssetCatalogs.AsNoTracking().AsSplitQuery().Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.GameVersion == gameVersion &&
                item.Kind == AssetImportJobValues.StaticMeshes && item.IsActive, cancellationToken);
        if (catalog is null)
        {
            return new StaticMeshLookup(
                new Dictionary<string, PublishedStaticMesh>(StringComparer.OrdinalIgnoreCase),
                []);
        }
        var meshes = catalog.Items.Select(item => JsonSerializer.Deserialize<StaticMeshManifestEntry>(item.MetadataJson, ManifestJsonOptions)!)
            .Where(mesh => mesh.Url is not null)
            .ToDictionary(
                mesh => MeshKey(mesh.PackageName, mesh.ObjectName),
                mesh => new PublishedStaticMesh(mesh.Url!, mesh.VertexCount),
                StringComparer.OrdinalIgnoreCase);
        var metadata = JsonSerializer.Deserialize<StaticMeshCatalogMetadata>(catalog.MetadataJson, ManifestJsonOptions);
        return new StaticMeshLookup(meshes, metadata?.GpuTextureFormats ?? []);
    }

    private static async Task<MapActorManifestEntry[]> BuildActorManifestsAsync(
        IReadOnlyList<UnrealLevelActor> sourceActors,
        StaticMeshLookup staticMeshes,
        string outputPath,
        string kind,
        MapSource source,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var actors = new MapActorManifestEntry[sourceActors.Count];
        var lightingGroups = new Dictionary<string, List<(int ActorIndex, IReadOnlyList<UnrealColor> Colors)>>(
            StringComparer.OrdinalIgnoreCase);
        for (var index = 0; index < sourceActors.Count; index++)
        {
            var actor = sourceActors[index];
            PublishedStaticMesh? publishedMesh = null;
            if (actor.StaticMesh is not null && !staticMeshes.Meshes.TryGetValue(
                MeshKey(actor.StaticMesh.PackageName, actor.StaticMesh.ObjectName),
                out publishedMesh))
            {
                warnings.Add($"{source.FileName}/{actor.Name}: static mesh '{actor.StaticMesh.Path}' is not published.");
            }
            if (actor.VertexLightingError is not null)
            {
                warnings.Add($"{source.FileName}/{actor.Name}: {actor.VertexLightingError}");
            }
            actors[index] = new MapActorManifestEntry(
                actor.Name,
                actor.ClassName,
                Vec(actor.Location),
                Rot(actor.Rotation),
                Vec(actor.PrePivot),
                actor.DrawScale,
                Vec(actor.DrawScale3D),
                actor.StaticMesh?.PackageName,
                actor.StaticMesh?.ObjectName,
                publishedMesh?.Url,
                null);
            if (publishedMesh is null || actor.VertexLighting is not { Count: > 0 }) continue;
            if (actor.VertexLighting.Count != publishedMesh.VertexCount)
            {
                warnings.Add(
                    $"{source.FileName}/{actor.Name}: instance lighting has {actor.VertexLighting.Count} colors " +
                    $"for a {publishedMesh.VertexCount}-vertex mesh and was ignored.");
                continue;
            }
            var key = MeshKey(actor.StaticMesh!.PackageName, actor.StaticMesh.ObjectName);
            if (!lightingGroups.TryGetValue(key, out var group)) lightingGroups[key] = group = [];
            group.Add((index, actor.VertexLighting));
        }

        foreach (var group in lightingGroups.Values)
        {
            var texelCount = group.Sum(item => item.Colors.Count);
            var width = Math.Min(2048, NextPowerOfTwo(Math.Min(texelCount, 2048)));
            var height = checked((texelCount + width - 1) / width);
            var pixels = new Rgba32[checked(width * height)];
            var offset = 0;
            foreach (var item in group)
            {
                foreach (var color in item.Colors)
                {
                    pixels[offset++] = new Rgba32(color.Red, color.Green, color.Blue, color.Alpha);
                }
            }
            var bytes = await WebpTextureEncoder.EncodeRgbaDataLosslessAsync(
                pixels,
                width,
                height,
                cancellationToken);
            var hash = Convert.ToHexStringLower(SHA256.HashData(bytes));
            var fileName = $"lighting-{hash[..16]}.webp";
            await File.WriteAllBytesAsync(Path.Combine(outputPath, fileName), bytes, cancellationToken);
            var url = VersionedUrl(kind, source.Name, fileName, hash, gpuTextureAvailable: false);
            offset = 0;
            foreach (var item in group)
            {
                actors[item.ActorIndex] = actors[item.ActorIndex] with
                {
                    VertexLighting = new MapVertexLightingReference(
                        url,
                        width,
                        height,
                        offset,
                        item.Colors.Count)
                };
                offset += item.Colors.Count;
            }
        }
        return actors;
    }

    private static int NextPowerOfTwo(int value)
    {
        var result = 1;
        while (result < value) result <<= 1;
        return result;
    }

    private static string MeshKey(string packageName, string objectName) => $"{packageName}\n{objectName}";
    private static string TextureKey(string packageName, string objectName) => $"{packageName}\n{objectName}";
    private static TextureMaterialReference? MaterialReference(
        string currentPackage,
        UnrealObjectReference? reference) => reference is null
        ? null
        : new TextureMaterialReference(
            string.IsNullOrEmpty(reference.PackageName) ? currentPackage : reference.PackageName,
            reference.ObjectName,
            reference.ClassName);

    private static TextureMaterialManifestEntry MaterialManifest(
        string packageName,
        UnrealMaterialExport material) => new(
            packageName,
            material.Name,
            material.ClassName,
            MaterialReference(packageName, material.Material),
            MaterialReference(packageName, material.Diffuse),
            MaterialReference(packageName, material.Opacity),
            MaterialReference(packageName, material.SelfIllumination),
            material.OutputBlending,
            material.FrameBufferBlending,
            material.TwoSided,
            material.AlphaTest,
            material.AlphaRef,
            material.ZWrite,
            material.ZTest,
            MaterialReference(packageName, material.Material2),
            MaterialReference(packageName, material.Mask),
            material.PanRate,
            material.RotationRate,
            material.CombineOperation,
            material.AlphaOperation,
            MaterialReference(packageName, material.Detail),
            material.DetailScale,
            material.ModifierColor is { } color
                ? new TextureMaterialColor(color.Red, color.Green, color.Blue, color.Alpha)
                : null,
            material.UOscillationType,
            material.VOscillationType,
            material.UOscillationRate,
            material.VOscillationRate,
            material.UOscillationAmplitude,
            material.VOscillationAmplitude,
            material.UOscillationPhase,
            material.VOscillationPhase,
            material.TreatAsTwoSided,
            MaterialReference(packageName, material.SelfIlluminationMask),
            MaterialReference(packageName, material.Specular),
            MaterialReference(packageName, material.SpecularityMask),
            material.PerformLightingOnSpecularPass,
            material.FadeColor1 is { } fadeColor1
                ? new TextureMaterialColor(fadeColor1.Red, fadeColor1.Green, fadeColor1.Blue, fadeColor1.Alpha)
                : null,
            material.FadeColor2 is { } fadeColor2
                ? new TextureMaterialColor(fadeColor2.Red, fadeColor2.Green, fadeColor2.Blue, fadeColor2.Alpha)
                : null,
            material.ColorFadeType,
            material.FadePeriod,
            material.FadePhase,
            material.InvertMask,
            material.Modulate2X,
            material.Modulate4X);
    private static string TextureMapAxis(byte value) => value switch
    {
        0 => "xy",
        1 => "xz",
        2 => "yz",
        _ => "unknown"
    };
    private static MapVector Vec(System.Numerics.Vector3 value) => new(value.X, value.Y, value.Z);
    private static MapRotation Rot(UnrealRotator value) => new(value.Pitch, value.Yaw, value.Roll);
    private static MapEnvironmentManifestEntry PublishEnvironment(UnrealLevel map)
    {
        var environment = map.Environment;
        if (environment is null)
        {
            return new MapEnvironmentManifestEntry(new MapColor(0, 0, 0), 0, null);
        }
        var ambient = environment.AmbientColor;
        MapDistanceFog? fog = null;
        if (environment.DistanceFog is { } distanceFog &&
            distanceFog.End > distanceFog.Start)
        {
            fog = new MapDistanceFog(
                Color(distanceFog.Color),
                distanceFog.Start,
                distanceFog.End);
        }
        return new MapEnvironmentManifestEntry(
            Color(ambient),
            environment.AmbientBrightness,
            fog);
    }

    private static MapColor Color(UnrealColor value) =>
        new(value.Red / 255f, value.Green / 255f, value.Blue / 255f);

    private static async Task<string> WriteManifestAsync<T>(
        string path,
        T manifest,
        CancellationToken cancellationToken)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(manifest, ManifestJsonOptions);
        var contents = new byte[json.Length + 1];
        json.CopyTo(contents, 0);
        contents[^1] = (byte)'\n';
        await File.WriteAllBytesAsync(path, contents, cancellationToken);
        return Convert.ToHexStringLower(SHA256.HashData(contents));
    }

}
