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

public sealed partial class AssetImportJobProcessor(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IAssetCatalogStore catalogStore,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider,
    ILogger<AssetImportJobProcessor> logger) : IAssetImportJobProcessor
{
    internal const int LevelSchemaVersion = 12;
    internal const int SceneSchemaVersion = 11;

    private static readonly JsonSerializerOptions ManifestJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    public async Task<bool> ProcessNextAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await ReconcilePromotionsAsync(context, cancellationToken);
        AssetImportJob? job;
        await using (var transaction = await context.Database.BeginTransactionAsync(cancellationToken))
        {
            job = await context.AssetImportJobs
                .FromSqlRaw(
                    "SELECT * FROM content.asset_import_jobs " +
                    "WHERE kind IN ('systextures', 'textures', 'music', 'sounds', 'staticmeshes', 'levels', 'levelpreviews', 'scenes') AND (status = 'queued' " +
                    "OR (status = 'running' AND started_at < NOW() - INTERVAL '15 minutes')) " +
                    "ORDER BY requested_at FOR UPDATE SKIP LOCKED LIMIT 1")
                .SingleOrDefaultAsync(cancellationToken);
            if (job is null)
            {
                await transaction.CommitAsync(cancellationToken);
                return false;
            }

            job.Status = AssetImportJobValues.Running;
            job.StartedAt = timeProvider.GetUtcNow();
            job.FinishedAt = null;
            job.TotalCount = 0;
            job.ProcessedCount = 0;
            job.SkippedCount = 0;
            job.WarningsJson = "[]";
            job.Error = null;
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        try
        {
            if (job.Kind == AssetImportJobValues.Music)
            {
                await ImportMusicAsync(context, job, cancellationToken);
            }
            else if (job.Kind == AssetImportJobValues.Sounds)
            {
                await ImportSoundsAsync(context, job, cancellationToken);
            }
            else if (job.Kind == AssetImportJobValues.StaticMeshes)
            {
                await ImportStaticMeshesAsync(context, job, cancellationToken);
            }
            else if (job.Kind == AssetImportJobValues.Levels)
            {
                await ImportLevelsAsync(context, job, cancellationToken);
            }
            else if (job.Kind == AssetImportJobValues.LevelPreviews)
            {
                await ImportLevelPreviewsAsync(context, job, cancellationToken);
            }
            else if (job.Kind == AssetImportJobValues.Scenes)
            {
                await ImportScenesAsync(context, job, cancellationToken);
            }
            else
            {
                await ImportTexturesAsync(context, job, cancellationToken);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            job.Status = AssetImportJobValues.Failed;
            job.FinishedAt = timeProvider.GetUtcNow();
            job.Error = exception.Message.Length <= 4000 ? exception.Message : exception.Message[..4000];
            await context.SaveChangesAsync(cancellationToken);
            logger.LogError(exception, "Asset import {JobId} failed", job.Id);
        }

        return true;
    }

}
