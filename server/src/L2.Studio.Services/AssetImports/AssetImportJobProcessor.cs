using System.Security.Cryptography;
using System.Data.Common;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Contracts;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using L2.Studio.Services.Interfaces;
using L2.Tools.AudioConverter;
using L2.Tools.ClientData;
using L2.Tools.PackageReader;
using L2.Tools.StaticMeshConverter;
using L2.Tools.TextureConverter;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PuppeteerSharp;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Services;

public sealed partial class AssetImportJobProcessor(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IAssetCatalogStore catalogStore,
    IDbContextOutbox outbox,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider,
    ILogger<AssetImportJobProcessor> logger) : IAssetImportWorkItemProcessor
{
    private readonly List<AssetCatalogDependencyPublication> dependencyHints = [];
    internal const int MapSchemaVersion = 16;
    internal const int SceneSchemaVersion = 13;

    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private static readonly JsonSerializerOptions CompactManifestJsonOptions = new(JsonSerializerDefaults.Web);

    public async Task ProcessAsync(Guid workItemId, CancellationToken cancellationToken)
    {
        dependencyHints.Clear();
        await using var executionLock = TryAcquireExecutionLock(workItemId);
        if (executionLock is null)
        {
            logger.LogDebug("Ignoring concurrent delivery of asset import work item {WorkItemId}", workItemId);
            return;
        }
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var item = await context.AssetImportWorkItems.Include(work => work.Run)
            .SingleOrDefaultAsync(work => work.Id == workItemId, cancellationToken);
        if (item is null) return;
        if (AssetImportJobValues.WorkItemTerminalStatuses.Contains(item.Status))
        {
            await PublishCompletionAsync(item, cancellationToken);
            return;
        }

        item.Status = AssetImportJobValues.Running;
        item.AttemptCount++;
        item.StartedAt = timeProvider.GetUtcNow();
        item.LastHeartbeatAt = item.StartedAt;
        item.Run.LastHeartbeatAt = item.StartedAt;
        item.FinishedAt = null;
        item.TotalResourceCount = 0;
        item.ProcessedResourceCount = 0;
        item.SkippedResourceCount = 0;
        item.WarningCount = 0;
        item.WarningsJson = "[]";
        item.Error = null;
        item.UnpublishedAt = null;
        item.Run.Status = AssetImportJobValues.Running;
        item.Run.StartedAt ??= item.StartedAt;
        await context.AssetImportDiagnostics.Where(diagnostic => diagnostic.WorkItemId == item.Id)
            .ExecuteDeleteAsync(cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await using var heartbeat = AssetImportHeartbeatLease.Start(
            contextFactory, timeProvider, item.RunId, item.Id, cancellationToken);

        var sourceStagingPath = Path.Combine(
            Path.GetFullPath(options.Value.SourceSnapshotRootPath), item.Id.ToString("N"));
        var outputStagingPath = Path.Combine(
            AssetWorkRoot(item), item.Id.ToString("N"));
        try
        {
            if (Directory.Exists(outputStagingPath)) Directory.Delete(outputStagingPath, recursive: true);
            if (item.ImportKind != AssetImportJobValues.MapPreviews)
            {
                ResetDirectory(sourceStagingPath);
                var snapshotPath = Path.Combine(sourceStagingPath, item.SourceKey);
                var currentHash = await SnapshotAndHashFileAsync(item.SourcePath, snapshotPath, cancellationToken);
                if (item.SourceHash is not null && !string.Equals(item.SourceHash, currentHash, StringComparison.Ordinal))
                    throw new InvalidDataException("The source changed after discovery; start a new import run.");
                item.SourceHash = currentHash;
                item.ConversionSourcePath = snapshotPath;
                await context.SaveChangesAsync(cancellationToken);
            }

            item.ArtifactFingerprint = await PreliminaryArtifactFingerprintAsync(context, item, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            if (item.ImportKind == AssetImportJobValues.Music)
                await ImportMusicAsync(context, item, cancellationToken);
            else if (item.ImportKind == AssetImportJobValues.Sounds)
                await ImportSoundsAsync(context, item, cancellationToken);
            else if (item.ImportKind == AssetImportJobValues.StaticMeshes)
                await ImportStaticMeshesAsync(context, item, cancellationToken);
            else if (item.ImportKind == AssetImportJobValues.Animations)
                await ImportAnimationsAsync(context, item, cancellationToken);
            else if (item.ImportKind == AssetImportJobValues.NpcAppearances)
                await ImportNpcAppearancesAsync(context, item, cancellationToken);
            else if (item.ImportKind == AssetImportJobValues.Maps)
                await ImportMapsAsync(context, item, cancellationToken);
            else if (item.ImportKind == AssetImportJobValues.MapPreviews)
                await ImportMapPreviewsAsync(context, item, cancellationToken);
            else if (item.ImportKind == AssetImportJobValues.Scenes)
                await ImportScenesAsync(context, item, cancellationToken);
            else
                await ImportTexturesAsync(context, item, cancellationToken);

            await PersistWarningsAsync(context, item, cancellationToken);
        }
        catch (Exception exception) when (IsConversionFailure(exception))
        {
            logger.LogError(exception, "Asset import work item {WorkItemId} failed", item.Id);
            await catalogStore.FailAsync(item.Id, Truncate(exception.Message), cancellationToken);
        }
        finally
        {
            if (Directory.Exists(sourceStagingPath)) Directory.Delete(sourceStagingPath, recursive: true);
        }
    }

    private static async Task<string> PreliminaryArtifactFingerprintAsync(
        GameContentDbContext context,
        AssetImportWorkItem item,
        CancellationToken cancellationToken)
    {
        if (item.SourceHash is null) throw new InvalidOperationException("The source hash is unavailable.");
        var previous = await context.AssetCatalogSources.AsNoTracking().Include(source => source.Dependencies)
            .SingleOrDefaultAsync(source => source.Catalog.GameVersion == item.GameVersion &&
                source.Catalog.Kind == item.ImportKind && source.Catalog.IsActive &&
                source.NormalizedSourceKey == item.NormalizedSourceKey, cancellationToken);
        if (previous is null) return ComputeArtifactFingerprint(item, []);
        var active = await context.AssetCatalogSources.AsNoTracking()
            .Where(source => source.Catalog.GameVersion == item.GameVersion && source.Catalog.IsActive)
            .Select(source => new { source.Catalog.Kind, source.NormalizedSourceKey, source.ArtifactFingerprint, source.SourceHash })
            .ToArrayAsync(cancellationToken);
        var dependencies = previous.Dependencies.Select(dependency =>
        {
            var current = dependency.ResolvedSourceKey is null ? null : active.FirstOrDefault(source =>
                source.Kind == dependency.Kind && source.NormalizedSourceKey == dependency.ResolvedSourceKey.ToLowerInvariant());
            return (dependency.Kind, dependency.DependencyKey,
                current?.ArtifactFingerprint ?? current?.SourceHash ?? "missing");
        });
        return ComputeArtifactFingerprint(item, dependencies);
    }

    private static string ComputeArtifactFingerprint(
        AssetImportWorkItem item,
        IEnumerable<(string Kind, string Key, string Fingerprint)> dependencies) =>
        item.ImportKind == AssetImportJobValues.MapPreviews
            ? MapPreviewGeneration.ArtifactFingerprint(
                item.SourceHash!, dependencies, item.Run.Force, item.RunId)
            : AssetArtifactFingerprint.Compute(item.ImportKind, item.SourceHash!, dependencies);

    private async Task PersistWarningsAsync(
        GameContentDbContext context,
        AssetImportWorkItem item,
        CancellationToken cancellationToken)
    {
        var warnings = JsonSerializer.Deserialize<string[]>(item.WarningsJson) ?? [];
        if (warnings.Length > 0)
        {
            foreach (var warning in warnings)
            {
                context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
                {
                    RunId = item.RunId,
                    WorkItemId = item.Id,
                    Severity = "warning",
                    Code = DiagnosticCode(item.ImportKind),
                    Stage = DiagnosticStage(item.ImportKind),
                    SourceKey = item.SourceKey,
                    Message = Truncate(warning),
                    CreatedAt = timeProvider.GetUtcNow()
                });
            }
            item.WarningCount = warnings.Length;
            item.Status = AssetImportJobValues.SucceededWithWarnings;
        }
        else if (item.Status != AssetImportJobValues.Failed)
        {
            item.Status = AssetImportJobValues.Succeeded;
        }
        item.FinishedAt ??= timeProvider.GetUtcNow();
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task PublishCompletionAsync(AssetImportWorkItem item, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        outbox.Enroll(context);
        await outbox.PublishAsync(new AssetImportWorkItemCompleted(item.RunId, item.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    private static async Task<string> SnapshotAndHashFileAsync(
        string sourcePath,
        string snapshotPath,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
        await using var source = new FileStream(
            sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 128, true);
        await using var snapshot = new FileStream(
            snapshotPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1024 * 128, true);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[1024 * 128];
        int count;
        while ((count = await source.ReadAsync(buffer, cancellationToken)) > 0)
        {
            hash.AppendData(buffer, 0, count);
            await snapshot.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }
        await snapshot.FlushAsync(cancellationToken);
        return Convert.ToHexStringLower(hash.GetHashAndReset());
    }

    private static void ResetDirectory(string path)
    {
        if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        Directory.CreateDirectory(path);
    }

    private FileStream? TryAcquireExecutionLock(Guid workItemId)
    {
        var lockRoot = Path.Combine(Path.GetFullPath(options.Value.SourceSnapshotRootPath), ".locks");
        Directory.CreateDirectory(lockRoot);
        try
        {
            return new FileStream(
                Path.Combine(lockRoot, $"{workItemId:N}.lock"),
                FileMode.OpenOrCreate,
                FileAccess.ReadWrite,
                FileShare.None);
        }
        catch (IOException)
        {
            return null;
        }
    }

    internal static string DiagnosticCode(string kind) => kind switch
    {
        AssetImportJobValues.Maps or AssetImportJobValues.Scenes => "map.resource_warning",
        AssetImportJobValues.MapPreviews => "preview.render_warning",
        AssetImportJobValues.StaticMeshes => "static_mesh.resource_warning",
        AssetImportJobValues.Animations => "animation.resource_warning",
        AssetImportJobValues.NpcAppearances => "npc_appearance.resource_warning",
        AssetImportJobValues.Music or AssetImportJobValues.Sounds => "audio.resource_warning",
        _ => "texture.resource_warning"
    };

    private static string DiagnosticStage(string kind) =>
        kind == AssetImportJobValues.MapPreviews ? "render" : "conversion";

    private static string Truncate(string value) => value.Length <= 4000 ? value : value[..4000];

    private static bool IsConversionFailure(Exception exception) =>
        exception is not OperationCanceledException &&
        exception is not DbException &&
        exception is not DbUpdateException &&
        exception.InnerException is not DbException;
}
