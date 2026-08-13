using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using L2.Studio.Context;
using L2.Studio.Context.Entities;
using L2.Studio.Messages;
using L2.Studio.Repositories.Interfaces;
using L2.Studio.Repositories.Interfaces.Models;
using Microsoft.EntityFrameworkCore;
using Wolverine.EntityFrameworkCore;
using Wolverine.Runtime;

namespace L2.Studio.Repositories;

public sealed class AssetCatalogStore(
    IDbContextFactory<GameContentDbContext> contextFactory,
    IDbContextOutbox outbox,
    TimeProvider timeProvider) : IAssetCatalogStore
{
    public async Task PublishAsync(AssetCatalogPublication publication, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var workItem = await context.AssetImportWorkItems.Include(item => item.Run)
            .SingleAsync(item => item.Id == publication.WorkItemId, cancellationToken);
        if (AssetImportJobValues.WorkItemTerminalStatuses.Contains(workItem.Status))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }
        ApplyBuildFingerprint(workItem, publication);

        var catalog = await context.AssetCatalogs.Include(item => item.Sources)
            .SingleOrDefaultAsync(item => item.GameVersion == publication.GameVersion &&
                item.Kind == publication.Kind && item.IsActive, cancellationToken);
        if (catalog is null)
        {
            catalog = new AssetCatalog
            {
                Id = Guid.NewGuid(),
                GameVersion = publication.GameVersion,
                Kind = publication.Kind,
                SourceFolder = publication.SourceFolder,
                SourceHash = publication.SourceHash,
                SchemaVersion = publication.SchemaVersion,
                Protocol = publication.Protocol,
                MetadataJson = publication.MetadataJson,
                IsActive = true,
                PublishedAt = publication.PublishedAt
            };
            context.AssetCatalogs.Add(catalog);
        }

        var previous = catalog.Sources.SingleOrDefault(source =>
            source.NormalizedSourceKey == publication.NormalizedSourceKey);
        if (previous is not null) context.AssetCatalogSources.Remove(previous);

        var artifact = await context.AssetArtifacts.Include(item => item.Files)
            .SingleOrDefaultAsync(item => item.GameVersion == publication.GameVersion &&
                item.Kind == publication.Kind && item.NormalizedSourceKey == publication.NormalizedSourceKey &&
                item.BuildFingerprint == publication.BuildFingerprint, cancellationToken);
        if (artifact is not null && (artifact.ContentHash != publication.ContentHash ||
            !SameFiles(artifact.Files, publication.Files)))
            throw new InvalidDataException(
                "The registered artifact does not match the files produced for its build fingerprint.");
        if (artifact is null)
        {
            artifact = new AssetArtifact
            {
                Id = Guid.NewGuid(),
                GameVersion = publication.GameVersion,
                Kind = publication.Kind,
                SourceKey = publication.SourceKey,
                NormalizedSourceKey = publication.NormalizedSourceKey,
                SourceHash = publication.SourceHash,
                RecipeVersion = publication.RecipeVersion,
                BuildFingerprint = publication.BuildFingerprint,
                ContentHash = publication.ContentHash,
                OutputRoot = publication.OutputRoot,
                SchemaVersion = publication.SchemaVersion,
                Protocol = publication.Protocol,
                FileCount = publication.Files.Count,
                SizeBytes = publication.Files.Sum(file => file.SizeBytes),
                IntegrityStatus = "healthy",
                LastVerifiedAt = publication.PublishedAt,
                PublishingWorkItemId = publication.WorkItemId,
                CreatedAt = publication.PublishedAt,
                Files = publication.Files.Select(file => new AssetArtifactFile
                {
                    RelativePath = file.RelativePath,
                    PublicPath = file.PublicPath,
                    Role = file.Role,
                    MediaType = file.MediaType,
                    SizeBytes = file.SizeBytes,
                    Sha256 = file.Sha256
                }).ToList()
            };
            var activeArtifacts = await context.AssetCatalogSources.AsNoTracking()
                .Where(item => item.Catalog.GameVersion == publication.GameVersion && item.Catalog.IsActive)
                .Select(item => new { item.Catalog.Kind, item.NormalizedSourceKey, item.ArtifactId })
                .ToArrayAsync(cancellationToken);
            artifact.Dependencies = publication.Dependencies.Select(dependency => new AssetArtifactDependency
            {
                Kind = dependency.Kind,
                DependencyKey = dependency.DependencyKey,
                ResolvedSourceKey = dependency.ResolvedSourceKey,
                ResolvedArtifactId = dependency.ResolvedSourceKey is null ? null : activeArtifacts
                    .FirstOrDefault(item => item.Kind == dependency.Kind &&
                        item.NormalizedSourceKey == dependency.ResolvedSourceKey)?.ArtifactId,
                BuildFingerprint = dependency.ArtifactFingerprint,
                IsResolved = dependency.IsResolved
            }).ToList();
            context.AssetArtifacts.Add(artifact);
        }

        var references = publication.Dependencies.Where(dependency => dependency.OutputRoot is not null)
            .Select(dependency => dependency.OutputRoot!).Distinct(StringComparer.Ordinal).Order().ToArray();
        var source = new AssetCatalogSource
        {
            Id = Guid.NewGuid(),
            Catalog = catalog,
            Artifact = artifact,
            PublishingWorkItemId = publication.WorkItemId,
            SourceKey = publication.SourceKey,
            NormalizedSourceKey = publication.NormalizedSourceKey,
            SourceHash = publication.SourceHash,
            ArtifactFingerprint = publication.BuildFingerprint,
            OutputRoot = publication.OutputRoot,
            MetadataJson = publication.MetadataJson,
            ReferencedOutputRootsJson = JsonSerializer.Serialize(references),
            PublishedAt = publication.PublishedAt
        };
        source.Dependencies = publication.Dependencies.Select(dependency => new AssetCatalogSourceDependency
        {
            Kind = dependency.Kind,
            DependencyKey = dependency.DependencyKey,
            ResolvedSourceKey = dependency.ResolvedSourceKey?.Trim().ToLowerInvariant(),
            ArtifactFingerprint = dependency.ArtifactFingerprint,
            IsResolved = dependency.IsResolved
        }).ToList();
        source.Groups = publication.Groups.Select(group => new AssetCatalogGroup
        {
            Catalog = catalog,
            Source = source,
            Name = group.Name,
            MetadataJson = group.MetadataJson
        }).ToList();
        source.Items = publication.Items.Select(item => new AssetCatalogItem
        {
            Catalog = catalog,
            Source = source,
            Name = item.Name,
            GroupName = item.GroupName,
            Status = item.Status!,
            MetadataJson = item.MetadataJson
        }).ToList();
        context.AssetCatalogSources.Add(source);
        await context.SaveChangesAsync(cancellationToken);

        await MarkDependentsStaleAsync(
            context,
            publication.GameVersion,
            publication.Kind,
            publication.NormalizedSourceKey,
            publication.Groups.SelectMany(group => publication.Items
                .Where(item => item.GroupName == group.Name)
                .Select(item => $"{group.Name}.{item.Name}"))
                .Append(publication.NormalizedSourceKey),
            publication.BuildFingerprint,
            publication.PublishedAt,
            cancellationToken);

        catalog.SourceHash = AggregateHash(catalog.Sources
            .Where(item => item.Id != previous?.Id && item.Id != source.Id)
            .Append(source)
            .Select(item => (item.NormalizedSourceKey, item.SourceHash)));
        catalog.SourceFolder = publication.SourceFolder;
        catalog.SchemaVersion = publication.SchemaVersion;
        catalog.Protocol = publication.Protocol;
        var activeSources = catalog.Sources
            .Where(item => item.Id != previous?.Id && item.Id != source.Id)
            .Append(source)
            .ToArray();
        catalog.MetadataJson = AssetCatalogMetadataAggregator.Aggregate(
            publication.Kind, activeSources.Select(item => item.MetadataJson));
        catalog.PublishedAt = publication.PublishedAt;
        workItem.Status = publication.Warnings.Count == 0
            ? AssetImportJobValues.Succeeded
            : AssetImportJobValues.SucceededWithWarnings;
        workItem.WarningCount = publication.Warnings.Count;
        workItem.FinishedAt = publication.PublishedAt;
        workItem.Error = null;
        workItem.UnpublishedAt = null;

        outbox.Enroll(context);
        await outbox.PublishAsync(new AssetImportWorkItemCompleted(workItem.RunId, workItem.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    internal static void ApplyBuildFingerprint(
        AssetImportWorkItem workItem,
        AssetCatalogPublication publication)
    {
        if (string.IsNullOrWhiteSpace(publication.BuildFingerprint))
            throw new InvalidDataException("The artifact build fingerprint is required.");
        var outputFingerprint = Path.GetFileName(publication.OutputRoot.TrimEnd('/', '\\'));
        if (!string.Equals(outputFingerprint, publication.BuildFingerprint, StringComparison.Ordinal))
            throw new InvalidDataException(
                "The artifact output directory does not match its build fingerprint.");
        workItem.ArtifactFingerprint = publication.BuildFingerprint;
    }

    public async Task FailAsync(Guid workItemId, string error, CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var workItem = await context.AssetImportWorkItems.Include(item => item.Run)
            .SingleOrDefaultAsync(item => item.Id == workItemId, cancellationToken);
        if (workItem is null || AssetImportJobValues.WorkItemTerminalStatuses.Contains(workItem.Status))
        {
            await transaction.CommitAsync(cancellationToken);
            return;
        }
        workItem.Status = AssetImportJobValues.Failed;
        workItem.FinishedAt = timeProvider.GetUtcNow();
        workItem.Error = error;
        context.AssetImportDiagnostics.Add(new AssetImportDiagnostic
        {
            RunId = workItem.RunId,
            WorkItemId = workItem.Id,
            Severity = "error",
            Code = workItem.SourceHash is null ? "discovery.source_unavailable" : "conversion.failed",
            Stage = "conversion",
            SourceKey = workItem.SourceKey,
            Message = error,
            CreatedAt = timeProvider.GetUtcNow()
        });
        outbox.Enroll(context);
        await outbox.PublishAsync(new AssetImportWorkItemCompleted(workItem.RunId, workItem.Id));
        await outbox.SaveChangesAndFlushMessagesAsync(MultiFlushMode.AllowMultiples, cancellationToken);
    }

    private static async Task MarkDependentsStaleAsync(
        GameContentDbContext context,
        string gameVersion,
        string kind,
        string normalizedSourceKey,
        IEnumerable<string> logicalKeys,
        string artifactFingerprint,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var keys = logicalKeys.Select(key => key.Trim().ToLowerInvariant()).ToHashSet(StringComparer.Ordinal);
        var dependencies = await context.AssetCatalogSourceDependencies.Include(dependency => dependency.Source)
            .ThenInclude(source => source.Catalog)
            .Where(dependency => dependency.Source.Catalog.GameVersion == gameVersion &&
                dependency.Source.Catalog.IsActive && dependency.Kind == kind &&
                (dependency.ResolvedSourceKey == normalizedSourceKey || keys.Contains(dependency.DependencyKey)))
            .ToListAsync(cancellationToken);
        foreach (var dependent in dependencies.Where(dependency => dependency.ArtifactFingerprint != artifactFingerprint))
        {
            dependent.Source.IsStale = true;
            dependent.Source.StaleAt ??= now;
            var reasons = JsonSerializer.Deserialize<List<string>>(dependent.Source.StaleReasonsJson) ?? [];
            var reason = $"Dependency {kind}:{dependent.DependencyKey} changed.";
            if (!reasons.Contains(reason, StringComparer.Ordinal)) reasons.Add(reason);
            dependent.Source.StaleReasonsJson = JsonSerializer.Serialize(reasons);
        }
    }

    internal static string AggregateHash(IEnumerable<(string SourceKey, string SourceHash)> sources)
    {
        var value = string.Join('\n', sources.OrderBy(item => item.SourceKey, StringComparer.Ordinal)
            .Select(item => $"{item.SourceKey}\0{item.SourceHash}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static bool SameFiles(
        IEnumerable<AssetArtifactFile> registered,
        IEnumerable<AssetArtifactFilePublication> published) =>
        registered.OrderBy(file => file.RelativePath, StringComparer.Ordinal)
            .Select(file => (file.RelativePath, file.SizeBytes, file.Sha256))
            .SequenceEqual(published.OrderBy(file => file.RelativePath, StringComparer.Ordinal)
                .Select(file => (file.RelativePath, file.SizeBytes, file.Sha256)));
}
