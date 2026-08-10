using System.Text.Json;
using L2.Studio.Content;
using L2.Studio.Content.Entities;
using L2.Studio.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;

namespace L2.Studio.Api.Assets;

public sealed class AssetImportRepository(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IOptions<AssetImportOptions> options,
    TimeProvider timeProvider)
{
    public async Task<AssetImportJob?> QueueAsync(
        string kind,
        string? levelName,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (levelName is not null)
        {
            var levelExists = await context.AssetCatalogItems.AsNoTracking().AnyAsync(
                item => item.Catalog.Kind == AssetImportJobValues.Levels &&
                    item.Catalog.IsActive && item.Name == levelName,
                cancellationToken);
            if (!levelExists)
            {
                throw new AssetImportTargetNotFoundException(levelName);
            }
        }

        var sourcePath = kind switch
        {
            AssetImportJobValues.SystemTextures => options.Value.SystemTexturesSourcePath,
            AssetImportJobValues.Textures => options.Value.TexturesSourcePath,
            AssetImportJobValues.Music => options.Value.MusicSourcePath,
            AssetImportJobValues.Sounds => options.Value.SoundsSourcePath,
            AssetImportJobValues.StaticMeshes => options.Value.StaticMeshesSourcePath,
            AssetImportJobValues.Levels => options.Value.LevelsSourcePath,
            AssetImportJobValues.LevelPreviews => options.Value.LevelsSourcePath,
            AssetImportJobValues.Scenes => options.Value.LevelsSourcePath,
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported asset import kind.")
        };
        if (levelName is not null)
        {
            sourcePath = Path.Combine(sourcePath, $"{levelName}.unr");
        }
        var active = await context.AssetImportJobs.AnyAsync(
            job => job.Kind == kind &&
                AssetImportJobValues.ActiveStatuses.Contains(job.Status),
            cancellationToken);
        if (active)
        {
            return null;
        }

        var job = new AssetImportJob
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            Status = AssetImportJobValues.Queued,
            SourcePath = sourcePath,
            RequestedAt = timeProvider.GetUtcNow(),
            WarningsJson = "[]"
        };
        context.AssetImportJobs.Add(job);
        try
        {
            await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "ix_asset_import_jobs_active_kind"
            })
        {
            return null;
        }

        return job;
    }

    public async Task<IReadOnlyList<AssetImportJobSummary>> GetRecentAsync(
        string kind,
        int limit,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var jobs = await context.AssetImportJobs
            .AsNoTracking()
            .Where(job => job.Kind == kind)
            .OrderByDescending(job => job.RequestedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
        return jobs.Select(ToSummary).ToArray();
    }

    public async Task<AssetImportJobSummary?> GetAsync(
        Guid id,
        string kind,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var job = await context.AssetImportJobs.AsNoTracking().SingleOrDefaultAsync(
            item => item.Id == id && item.Kind == kind,
            cancellationToken);
        return job is null ? null : ToSummary(job);
    }

    public static AssetImportJobSummary ToSummary(AssetImportJob job) => new(
        job.Id,
        job.Kind,
        job.Status,
        job.SourcePath,
        job.SourceHash,
        job.RequestedAt,
        job.StartedAt,
        job.FinishedAt,
        job.TotalCount,
        job.ProcessedCount,
        job.SkippedCount,
        JsonSerializer.Deserialize<string[]>(job.WarningsJson) ?? [],
        job.Error);
}

public sealed class AssetImportTargetNotFoundException(string levelName)
    : Exception($"The level '{levelName}' does not exist in the active level catalog.");
